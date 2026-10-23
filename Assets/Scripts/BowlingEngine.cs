using System.Collections.Generic;
using UnityEngine;

public enum DeliveryLength { Yorker, Full, GoodLength, BackOfLength, Short, FullToss }

[System.Serializable]
public struct BowlingPoint
{
    public string name;
    public Vector3 point;
    public DeliveryLength length;
}



[ExecuteInEditMode]
public class BowlingEngine : MonoBehaviour
{
    [Header("References")]
    public Rigidbody ballRigidbody;
    public Transform ballReleasePoint, marker;

    [Header("Speed Settings")]
    [Tooltip("Target speed in km/h (e.g., 145 for Fast, 120 for Medium)")]
    public float deliverySpeedKmh = 145f;

    [Header("Delivery Points Data")]
    public List<BowlingPoint> deliveryPoints = new List<BowlingPoint>();

    public static BowlingEngine instance;
    [HideInInspector] public Vector3 chosenPoint;
    public int deliveryIndex;

    private void Awake()
    {
        instance = this;
        if (deliveryPoints == null || deliveryPoints.Count == 0)
        {
            InitializePoints();
        }
    }   

    public Vector3 DecidePoint(bool isDeathOvers)
    {
        BowlingPoint targetZone = SelectTargetZoneRealistic(isDeathOvers);

        // Add minor variation (Spray)
        chosenPoint = targetZone.point + new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));

        Debug.Log($"<color=cyan>CHOSEN:</color> <b>{targetZone.name}</b> | Target Pos: {targetZone.point} | Speed: {deliverySpeedKmh}km/h");

        if (marker != null) marker.position = chosenPoint;
        return chosenPoint;
    }

    public void Release()
    {
        if (!Application.isPlaying) return;

        float speedMs = deliverySpeedKmh / 3.6f;

        // DO NOT overwrite chosenPoint here.
        // chosenPoint = Gameplay.instance.pitchPoint; // <-- REMOVED

        Vector3 launchVelocity = CalculateVelocityForSpeed(chosenPoint, speedMs);

        ballRigidbody.isKinematic = true;
        ballRigidbody.transform.position = ballReleasePoint.position;
        ballRigidbody.transform.rotation = ballReleasePoint.rotation;
        ballRigidbody.isKinematic = false;

        ballRigidbody.linearVelocity = launchVelocity;
        ballRigidbody.angularVelocity = Vector3.zero;

        Debug.Log($"BOWLING LAUNCH | Configured: {deliverySpeedKmh:F1} km/h | Calculated velocity: {launchVelocity.magnitude * 3.6f:F1} km/h | Velocity vector: {launchVelocity}");

        ballRigidbody.GetComponent<BallHit>()?.StartSpeedDebug();
    }

    [ContextMenu("Log Points for Script")]
    public void LogPointsForScript()
    {
        string code = "public void InitializePoints()\n{\n    deliveryPoints.Clear();\n    float groundY = 0.036f;\n";
        foreach (var dp in deliveryPoints)
        {
            code += $"    AddPoint(\"{dp.name}\", new Vector3({dp.point.x:F2}f, {dp.point.y:F2}f, {dp.point.z:F2}f), DeliveryLength.{dp.length});\n";
        }
        code += "}";
        Debug.Log(code);
    }

    private Vector3 CalculateVelocityForSpeed(Vector3 target, float speed)
    {
        Vector3 origin = ballReleasePoint.position;
        Vector3 toTarget = target - origin;

        // Calculate time based on 3D distance and speed
        float distance = toTarget.magnitude;
        float time = distance / speed;

        // X and Z components (Constant velocity)
        float vx = toTarget.x / time;
        float vz = toTarget.z / time;

        // Y component (Accounting for gravity)
        // Formula: y = v0y*t + 0.5*g*t^2  => v0y = (y - 0.5*g*t^2) / t
        float vy = (toTarget.y - (0.5f * Physics.gravity.y * time * time)) / time;

        return new Vector3(vx, vy, vz);
    }

    private BowlingPoint SelectTargetZoneRealistic(bool isDeathOvers)
    {
        List<BowlingPoint> pool = new List<BowlingPoint>();
        float r = Random.value;
        DeliveryLength chosenLength;

        if (isDeathOvers)
        {
            if (r < 0.65f) chosenLength = DeliveryLength.Yorker;
            else if (r < 0.85f) chosenLength = DeliveryLength.FullToss;
            else chosenLength = DeliveryLength.Full;
        }
        else
        {
            if (r < 0.75f) chosenLength = DeliveryLength.GoodLength;
            else if (r < 0.90f) chosenLength = DeliveryLength.BackOfLength;
            else chosenLength = DeliveryLength.Short;
        }

        pool = deliveryPoints.FindAll(p => p.length == chosenLength);
        if (pool.Count == 0) pool = deliveryPoints;

        //return pool[Random.Range(0, pool.Count)];
        return deliveryPoints[Random.Range(0, deliveryPoints.Count)];
        //return deliveryPoints[deliveryIndex];
    }

    //[ContextMenu("Reset to Default Points")]
    //public void InitializePoints()
    //{
    //    deliveryPoints.Clear();
    //    //float groundY = 0.036f; // ball radius

    //    //// YORKERS (just in front of stumps at Z = 8.1729)
    //    //AddPoint("Toe-Crusher Straight", new Vector3(0.00f, groundY, 8.00f), DeliveryLength.Yorker);
    //    //AddPoint("Off-Stump Blockhole", new Vector3(-0.104f, groundY, 7.80f), DeliveryLength.Yorker);
    //    //AddPoint("Wide Yorker (Legal)", new Vector3(-0.76f, groundY, 7.60f), DeliveryLength.Yorker);

    //    //// FULL (Z = 5.5 to 7.0)
    //    //AddPoint("Full at Stumps", new Vector3(-0.14f, groundY, 6.80f), DeliveryLength.Full);
    //    //AddPoint("Full Outside Off", new Vector3(-0.31f, groundY, 6.00f), DeliveryLength.Full);
    //    //AddPoint("Driving Full Length", new Vector3(-0.42f, groundY, 5.50f), DeliveryLength.Full);

    //    //// GOOD LENGTH (Z = 3.0 to 5.0)
    //    //AddPoint("Top of Off", new Vector3(-0.28f, groundY, 4.50f), DeliveryLength.GoodLength);
    //    //AddPoint("Fourth Stump Probe", new Vector3(-0.42f, groundY, 4.00f), DeliveryLength.GoodLength);
    //    //AddPoint("Corridor Control", new Vector3(-0.55f, groundY, 3.50f), DeliveryLength.GoodLength);
    //    //AddPoint("Into Pads Length", new Vector3(0.04f, groundY, 4.20f), DeliveryLength.GoodLength);

    //    //// BACK OF A LENGTH (Z = 1.5 to 3.0)
    //    //AddPoint("Heavy Back Length", new Vector3(-0.38f, groundY, 2.50f), DeliveryLength.BackOfLength);
    //    //AddPoint("Rising Corridor Length", new Vector3(-0.57f, groundY, 2.00f), DeliveryLength.BackOfLength);
    //    //AddPoint("Cramping Length", new Vector3(0.12f, groundY, 1.80f), DeliveryLength.BackOfLength);

    //    //// SHORT (Z = 0 to -2.0)
    //    //AddPoint("Head-High Bouncer", new Vector3(-0.42f, groundY, -0.50f), DeliveryLength.Short);
    //    //AddPoint("Rib-Cage Bouncer", new Vector3(-0.34f, groundY, -1.00f), DeliveryLength.Short);
    //    //AddPoint("Wide Surprise Bouncer", new Vector3(-0.84f, groundY, -1.80f), DeliveryLength.Short);
    //    //AddPoint("Nasty Throat Ball", new Vector3(-0.39f, groundY, -1.20f), DeliveryLength.Short);

    //    //// FULL TOSS (Y above ground, Z at stumps)
    //    //AddPoint("Full Toss at Stumps", new Vector3(0.00f, 0.60f, 8.17f), DeliveryLength.FullToss);
    //    //AddPoint("Full Toss Outside Off", new Vector3(-0.30f, 0.80f, 8.17f), DeliveryLength.FullToss);
    //    float groundY = 0.001f;

    //    // YORKERS
    //    AddPoint("Toe-Crusher Straight", new Vector3(0.00f, groundY, 8.30f), DeliveryLength.Yorker);
    //    AddPoint("Off-Stump Blockhole", new Vector3(-0.104f, groundY, 8.00f), DeliveryLength.Yorker);
    //    AddPoint("Wide Yorker (Legal)", new Vector3(-0.76f, groundY, 7.80f), DeliveryLength.Yorker);

    //    // FULL
    //    AddPoint("Full at Stumps", new Vector3(-0.14f, groundY, 6.00f), DeliveryLength.Full);
    //    AddPoint("Full Outside Off (4th stump)", new Vector3(-0.31f, groundY, 5.20f), DeliveryLength.Full);
    //    AddPoint("Driving Full Length (5th stump)", new Vector3(-0.42f, groundY, 4.00f), DeliveryLength.Full);

    //    // GOOD LENGTH
    //    AddPoint("Top of Off", new Vector3(-0.28f, groundY, 2.70f), DeliveryLength.GoodLength);
    //    AddPoint("Fourth Stump Probe", new Vector3(-0.42f, groundY, 2.30f), DeliveryLength.GoodLength);
    //    AddPoint("Corridor Control", new Vector3(-0.55f, groundY, 2.00f), DeliveryLength.GoodLength);
    //    AddPoint("Into Pads Length", new Vector3(0.04f, groundY, 2.50f), DeliveryLength.GoodLength);

    //    // BACK OF A LENGTH
    //    AddPoint("Heavy Back Length", new Vector3(-0.38f, groundY, 1.60f), DeliveryLength.BackOfLength);
    //    AddPoint("Rising Corridor Length", new Vector3(-0.57f, groundY, 1.30f), DeliveryLength.BackOfLength);
    //    AddPoint("Cramping Length", new Vector3(0.12f, groundY, 1.10f), DeliveryLength.BackOfLength);

    //    // SHORT
    //    AddPoint("Head-High Bouncer", new Vector3(-0.42f, groundY, -1.20f), DeliveryLength.Short);
    //    AddPoint("Rib-Cage Bouncer", new Vector3(-0.34f, groundY, -2.00f), DeliveryLength.Short);
    //    AddPoint("Wide Surprise Bouncer", new Vector3(-0.84f, groundY, -3.10f), DeliveryLength.Short);
    //    AddPoint("Nasty Throat Ball", new Vector3(-0.39f, groundY, -2.40f), DeliveryLength.Short);
    //}

    [ContextMenu("Reset to Default Points")]
    public void InitializePoints()
    {
        deliveryPoints.Clear();
        float groundY = 0.036f; // ball radius

        // YORKERS — just in front of the batsman's stumps (z = 10.06)
        AddPoint("Toe-Crusher Straight", new Vector3(0.00f, groundY, 9.85f), DeliveryLength.Yorker);
        AddPoint("Off-Stump Blockhole", new Vector3(-0.10f, groundY, 9.70f), DeliveryLength.Yorker);
        AddPoint("Wide Yorker (Legal)", new Vector3(-0.70f, groundY, 9.55f), DeliveryLength.Yorker);

        // FULL — 0.5–2.5m in front of stumps
        AddPoint("Full at Stumps", new Vector3(-0.14f, groundY, 8.90f), DeliveryLength.Full);
        AddPoint("Full Outside Off (4th stump)", new Vector3(-0.30f, groundY, 8.20f), DeliveryLength.Full);
        AddPoint("Driving Full Length (5th)", new Vector3(-0.42f, groundY, 7.70f), DeliveryLength.Full);

        // GOOD LENGTH — 2.5–5.5m in front
        AddPoint("Top of Off", new Vector3(-0.28f, groundY, 6.20f), DeliveryLength.GoodLength);
        AddPoint("Fourth Stump Probe", new Vector3(-0.42f, groundY, 5.60f), DeliveryLength.GoodLength);
        AddPoint("Corridor Control", new Vector3(-0.55f, groundY, 4.90f), DeliveryLength.GoodLength);
        AddPoint("Into Pads Length", new Vector3(0.04f, groundY, 5.80f), DeliveryLength.GoodLength);

        // BACK OF LENGTH — 5.5–7.5m in front
        AddPoint("Heavy Back Length", new Vector3(-0.38f, groundY, 3.80f), DeliveryLength.BackOfLength);
        AddPoint("Rising Corridor Length", new Vector3(-0.57f, groundY, 3.10f), DeliveryLength.BackOfLength);
        AddPoint("Cramping Length", new Vector3(0.12f, groundY, 3.40f), DeliveryLength.BackOfLength);

        // SHORT — 7.5–10m in front
        AddPoint("Head-High Bouncer", new Vector3(-0.42f, groundY, 1.20f), DeliveryLength.Short);
        AddPoint("Rib-Cage Bouncer", new Vector3(-0.34f, groundY, 0.40f), DeliveryLength.Short);
        AddPoint("Wide Surprise Bouncer", new Vector3(-0.84f, groundY, -0.60f), DeliveryLength.Short);
        AddPoint("Nasty Throat Ball", new Vector3(-0.39f, groundY, 0.80f), DeliveryLength.Short);

        // FULL TOSS — no bounce, at the stumps, above ground
        AddPoint("Full Toss at Stumps", new Vector3(0.00f, 0.60f, 10.06f), DeliveryLength.FullToss);
        AddPoint("Full Toss Outside Off", new Vector3(-0.30f, 0.80f, 10.06f), DeliveryLength.FullToss);
    }

    private void AddPoint(string n, Vector3 pos, DeliveryLength l)
    {
        deliveryPoints.Add(new BowlingPoint
        {
            name = n,
            point = pos,
            length = l
        });
    }


    public void UpdatePoint(int index, Vector3 newPos)
    {
        if (index >= 0 && index < deliveryPoints.Count)
        {
            BowlingPoint p = deliveryPoints[index];
            p.point = newPos;
            deliveryPoints[index] = p;
        }
    }
}