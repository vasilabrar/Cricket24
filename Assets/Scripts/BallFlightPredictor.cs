using System.Collections.Generic;
using UnityEngine;

public static class BallFlightPredictor
{
    public struct PathPoint
    {
        public float time;
        public Vector3 position;
        public Vector3 velocity;
        public bool grounded;
    }

    public class Prediction
    {
        public List<PathPoint> points = new List<PathPoint>(1024);
        public float totalTime;
        public int firstGroundIndex = -1;
        public bool leftField;
        public Vector3 exitPoint;
        public Vector3 lastRestPoint;

        public float FirstBounceTime => firstGroundIndex < 0 ? -1f : points[firstGroundIndex].time;
        public Vector3 FirstBouncePos => firstGroundIndex < 0 ? Vector3.zero : points[firstGroundIndex].position;

        public PathPoint SampleAt(float t)
        {
            if (points.Count == 0) return default;
            if (t <= points[0].time) return points[0];
            if (t >= points[points.Count - 1].time) return points[points.Count - 1];

            int lo = 0, hi = points.Count - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (points[mid].time <= t) lo = mid; else hi = mid;
            }
            var a = points[lo]; var b = points[hi];
            float u = (t - a.time) / Mathf.Max(1e-6f, b.time - a.time);
            return new PathPoint
            {
                time = t,
                position = Vector3.Lerp(a.position, b.position, u),
                velocity = Vector3.Lerp(a.velocity, b.velocity, u),
                grounded = a.grounded || b.grounded
            };
        }

        public Vector3 PositionAt(float t) => SampleAt(t).position;

        public bool TryGetIntercept(Vector3 fielderPos, float speed, float reach,
                            float reaction, float maxCatchHeight,
                            out float interceptTime, out Vector3 interceptPoint)
        {
            interceptTime = 0f;
            interceptPoint = Vector3.zero;
            if (points.Count == 0) return false;

            Vector3 fp = new Vector3(fielderPos.x, 0f, fielderPos.z);
            float best = float.MaxValue;

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p.position.y > maxCatchHeight) continue;

                // NEW: skip points where the ball is still rising. A fielder never
                // tries to cut off a ball that's climbing — he goes to where it
                // will be on the way down.
                if (p.velocity.y > 0.3f) continue;

                float eff = reach;
                if (p.position.y > 1.8f) eff *= 0.55f;

                Vector3 bp = new Vector3(p.position.x, 0f, p.position.z);
                float d = Vector3.Distance(fp, bp);
                float arrive = reaction + Mathf.Max(0f, (d - eff) / speed);

                if (arrive <= p.time && p.time < best)
                {
                    best = p.time;
                    interceptPoint = p.position;
                }
            }

            if (best == float.MaxValue) return false;
            interceptTime = best;
            return true;
        }
    }

    public static Prediction Predict(Vector3 startPos, Vector3 startVel,
                                     float ballRadius, float drag,
                                     float bounciness, float friction,
                                     float groundY, Bounds fieldBounds,
                                     float maxTime = 12f)
    {
        float dt = Time.fixedDeltaTime;
        const float sampleEvery = 0.02f;

        var res = new Prediction();
        Vector3 pos = startPos;
        Vector3 vel = startVel;
        Vector3 gravity = Physics.gravity;

        float t = 0f;
        float nextSample = 0f;
        bool grounded = false;
        int safety = 0;

        res.points.Add(new PathPoint { time = 0f, position = pos, velocity = vel });

        while (t < maxTime && safety++ < 20000)
        {
            vel += gravity * dt;
            if (drag > 0f) vel /= (1f + drag * dt);
            pos += vel * dt;
            t += dt;

            // --- bounce with tangential friction ---
            if (pos.y - ballRadius <= groundY)
            {
                pos.y = groundY + ballRadius;

                if (vel.y < 0f)
                {
                    float vn = -vel.y;
                    vel.y = vn * bounciness;
                    Vector3 horiz = new Vector3(vel.x, 0f, vel.z);
                    float hMag = horiz.magnitude;
                    if (hMag > 0.001f)
                    {
                        float loss = friction * (1f + bounciness) * vn;
                        float newHMag = Mathf.Max(0f, hMag - loss);
                        Vector3 newHoriz = horiz * (newHMag / hMag);
                        vel.x = newHoriz.x;
                        vel.z = newHoriz.z;
                    }

                    if (vel.y < 0.4f) vel.y = 0f;

                    if (!grounded)
                    {
                        grounded = true;
                        res.firstGroundIndex = res.points.Count;
                    }
                }
            }

            // --- boundary ---
            if (pos.y < 4f)
            {
                Vector3 flat = new Vector3(pos.x, fieldBounds.center.y, pos.z);
                if (!fieldBounds.Contains(flat))
                {
                    res.leftField = true;
                    res.exitPoint = pos;
                    res.points.Add(new PathPoint { time = t, position = pos, velocity = vel, grounded = grounded });
                    break;
                }
            }

            if (t >= nextSample)
            {
                res.points.Add(new PathPoint { time = t, position = pos, velocity = vel, grounded = grounded });
                nextSample += sampleEvery;
            }

            if (grounded && vel.sqrMagnitude < 0.05f && pos.y - ballRadius <= groundY + 0.1f)
            {
                res.points.Add(new PathPoint { time = t, position = pos, velocity = Vector3.zero, grounded = true });
                break;
            }
        }

        res.totalTime = t;
        res.lastRestPoint = res.points[res.points.Count - 1].position;
        return res;
    }
}