//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class FieldManager : MonoBehaviour
//{
//    public List<Fielder> fielders;
//    public List<Fielder> bestFielders = new List<Fielder>();

//    public bool tryingPickup, ballWasAirBorne;

//    public float fieldingRange = 1.5f;
//    public float score;

//    public static Vector3 hitBallPos, hitVelocity;

//    public Transform ball, marker, stumps;

//    public Fielder keeper;

//    [SerializeField] float fielderReactionTime = 0.12f;
//    [SerializeField] float groundCollectionReach = 0.45f;
//    [SerializeField] float aerialMinHeightRatio = 0.3f;
//    [SerializeField] float aerialMaxHeightRatio = 1.15f;
//    [SerializeField] float aerialReachRatio = 0.15f;

//    [SerializeField] MeshRenderer ignoreBounds;

//    private Coroutine keeperRunCoroutine;

//    public static Action<Fielder> StopOther;
//    public static Action<Vector3> StartCheckField;
//    public static Action ResetFielder, StopField;
//    public static Action<Vector3, Transform, bool> KeeperCollect;

//    private void OnEnable()
//    {
//        StartCheckField += AssignBestFielders;
//        ResetFielder += ResetFielders;
//        StopField += StopFielders;
//        StopOther += StopOtherFiedlers;
//        KeeperCollect += KeeperCollection;
//    }

//    private void OnDisable()
//    {
//        StartCheckField -= AssignBestFielders;
//        ResetFielder -= ResetFielders;
//        StopField -= StopFielders;
//        StopOther -= StopOtherFiedlers;
//        KeeperCollect -= KeeperCollection;
//    }

//    void KeeperCollection(Vector3 catchPoint, Transform ball, bool edge)
//    {
//        keeper.KeeperRecieve(catchPoint, ball, edge);
//    }

//    struct TrajectoryPoint
//    {
//        public Vector3 position;
//        public float time;

//        public TrajectoryPoint(Vector3 position, float time)
//        {
//            this.position = position;
//            this.time = time;
//        }
//    }

//    public void AssignBestFielders(Vector3 ballAt)
//    {
//        bestFielders.Clear();
//        ball = Gameplay.instance.currentBall;
//        hitBallPos = ballAt;

//        if (ball == null)
//        {
//            Debug.LogWarning("FieldManager.AssignBestFielders: currentBall is null");
//            return;
//        }

//        StartCoroutine(AssignBestFieldersAfterPhysics());
//    }

//    IEnumerator AssignBestFieldersAfterPhysics()
//    {
//        yield return new WaitForFixedUpdate();

//        if (ball == null || keeper == null)
//            yield break;

//        BallHit ballComp = ball.GetComponent<BallHit>();
//        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

//        if (ballComp == null || ballRb == null)
//        {
//            Debug.LogWarning("FieldManager: ball is missing BallHit or Rigidbody");
//            yield break;
//        }

//        hitVelocity = ballRb.linearVelocity;

//        float groundY = GetGroundY();

//        List<TrajectoryPoint> trajectory = PredictTrajectory(
//            ball.position,
//            ballRb.linearVelocity,
//            ballRb.linearDamping,
//            groundY
//        );

//        if (trajectory.Count == 0)
//            yield break;

//        Vector3 groundCenter = Gameplay.instance.stadiumBounds.center;

//        float groundRadius = Mathf.Min(
//            Gameplay.instance.stadiumBounds.extents.x,
//            Gameplay.instance.stadiumBounds.extents.z
//        );

//        Debug.Log($"========== FIELDER SELECTION START | Ball: {hitBallPos} | Velocity: {hitVelocity} | Candidates: {fielders.Count} | Trajectory Points: {trajectory.Count} ==========");

//        Fielder primary = null;
//        Vector3 primaryTarget = Vector3.zero;
//        Vector3 primaryBallPoint = Vector3.zero;
//        float primaryTime = float.MaxValue;
//        float primaryMargin = float.NegativeInfinity;
//        float primaryScore = float.NegativeInfinity;
//        bool primaryIsAerial = false;
//        bool keeperSelected = false;

//        // ---------- Outfielders ----------
//        foreach (Fielder fielder in fielders)
//        {
//            if (fielder == null || fielder == keeper || fielder.isKeeper)
//                continue;

//            if (!TryGetBestInterception(
//                fielder,
//                trajectory,
//                groundCenter,
//                groundRadius,
//                groundY,
//                0.15f,
//                false,
//                out Vector3 target,
//                out Vector3 ballPoint,
//                out bool isAerial,
//                out float ballTime,
//                out float margin))
//            {
//                Debug.Log($"FIELDER EVALUATED | {fielder.name} | Position: {fielder.transform.position} | No reachable trajectory point");
//                continue;
//            }

//            float candidateScore = -ballTime + margin * 0.3f;

//            Debug.Log($"FIELDER EVALUATED | {fielder.name} | Target: {target} | Ball Point: {ballPoint} | Type: {(isAerial ? "AERIAL" : "GROUND")} | Ball Time: {ballTime:F2}s | Margin: {margin:F2}s | Score: {candidateScore:F2}");

//            if (primary == null || candidateScore > primaryScore)
//            {
//                primary = fielder;
//                primaryTarget = target;
//                primaryBallPoint = ballPoint;
//                primaryIsAerial = isAerial;
//                primaryTime = ballTime;
//                primaryMargin = margin;
//                primaryScore = candidateScore;
//                keeperSelected = false;
//            }
//        }

//        // ---------- Keeper ----------
//        if (TryGetBestInterception(
//            keeper,
//            trajectory,
//            groundCenter,
//            groundRadius,
//            groundY,
//            0.15f,
//            true,
//            out Vector3 keeperTarget,
//            out Vector3 keeperBallPoint,
//            out bool keeperIsAerial,
//            out float keeperBallTime,
//            out float keeperMargin))
//        {
//            float keeperScore = keeperMargin - keeperBallTime * 0.1f;

//            Debug.Log($"KEEPER EVALUATED | {keeper.name} | Target: {keeperTarget} | Ball Point: {keeperBallPoint} | Type: {(keeperIsAerial ? "AERIAL" : "GROUND")} | Ball Time: {keeperBallTime:F2}s | Margin: {keeperMargin:F2}s | Score: {keeperScore:F2}");

//            if (primary == null || keeperScore > primaryScore)
//            {
//                primary = keeper;
//                primaryTarget = keeperTarget;
//                primaryBallPoint = keeperBallPoint;
//                primaryIsAerial = keeperIsAerial;
//                primaryTime = keeperBallTime;
//                primaryMargin = keeperMargin;
//                primaryScore = keeperScore;
//                keeperSelected = true;
//            }
//        }
//        else
//        {
//            Debug.Log($"KEEPER EVALUATED | {keeper.name} | No reachable trajectory point within keeper's collection zone");
//        }

//        if (primary == null)
//        {
//            Debug.LogWarning("FIELDER SELECTION | No fielder can reach the predicted trajectory");
//            StartKeeperRun();
//            yield break;
//        }

//        Debug.Log($"PRIMARY SELECTION | {primary.name} | Role: {(keeperSelected ? "KEEPER" : "OUTFIELDER")} | Type: {(primaryIsAerial ? "AERIAL" : "GROUND")} | Target: {primaryTarget} | Ball Point: {primaryBallPoint} | Ball Time: {primaryTime:F2}s | Margin: {primaryMargin:F2}s");

//        if (keeperSelected)
//        {
//            StopKeeperRun();
//            keeper.StopAllCoroutines();
//            bestFielders.Add(keeper);
//            keeper.interceptionPoint = primaryBallPoint;
//            keeper.isAerialInterception = primaryIsAerial;
//            keeper.startedRun = true;
//            keeper.KeeperRecieve(primaryBallPoint, ball, true);
//        }
//        else
//        {
//            AssignFielder(primary, primaryTarget, primaryBallPoint, primaryIsAerial);
//        }

//        // ---------- Backup ----------
//        Fielder backup = null;
//        Vector3 backupTarget = Vector3.zero;
//        Vector3 backupBallPoint = Vector3.zero;
//        float backupTime = float.MaxValue;
//        float backupMargin = float.NegativeInfinity;
//        float backupScore = float.NegativeInfinity;
//        bool backupIsAerial = false;

//        foreach (Fielder fielder in fielders)
//        {
//            if (fielder == null || fielder == keeper || fielder.isKeeper || fielder == primary)
//                continue;

//            if (!TryGetBestInterception(
//                fielder,
//                trajectory,
//                groundCenter,
//                groundRadius,
//                groundY,
//                primaryTime + 0.3f,
//                false,
//                out Vector3 target,
//                out Vector3 ballPoint,
//                out bool isAerial,
//                out float ballTime,
//                out float margin))
//            {
//                continue;
//            }

//            float candidateScore = margin - ballTime * 0.1f;

//            Debug.Log($"BACKUP CANDIDATE | {fielder.name} | Target: {target} | Ball Point: {ballPoint} | Type: {(isAerial ? "AERIAL" : "GROUND")} | Ball Time: {ballTime:F2}s | Margin: {margin:F2}s | Score: {candidateScore:F2}");

//            if (candidateScore > backupScore)
//            {
//                backup = fielder;
//                backupTarget = target;
//                backupBallPoint = ballPoint;
//                backupIsAerial = isAerial;
//                backupTime = ballTime;
//                backupMargin = margin;
//                backupScore = candidateScore;
//            }
//        }

//        if (backup != null && backupTime < trajectory[trajectory.Count - 1].time)
//        {
//            AssignFielder(backup, backupTarget, backupBallPoint, backupIsAerial);

//            Debug.Log($"BACKUP SELECTED | {backup.name} | Type: {(backupIsAerial ? "AERIAL" : "GROUND")} | Target: {backupTarget} | Ball Point: {backupBallPoint} | Ball Time: {backupTime:F2}s | Margin: {backupMargin:F2}s");
//        }

//        if (!keeperSelected)
//            StartKeeperRun();

//        Debug.Log($"========== FIELDER SELECTION COMPLETE | Assigned: {bestFielders.Count} | {(bestFielders.Count > 0 ? string.Join(", ", bestFielders.ConvertAll(f => f.name)) : "NONE")} ==========");
//    }

//    bool TryGetBestInterception(
//        Fielder fielder,
//        List<TrajectoryPoint> trajectory,
//        Vector3 groundCenter,
//        float groundRadius,
//        float groundY,
//        float minimumBallTime,
//        bool keeperMode,
//        out Vector3 bestTarget,
//        out Vector3 bestBallPoint,
//        out bool bestIsAerial,
//        out float bestBallTime,
//        out float bestMargin)
//    {
//        bestTarget = Vector3.zero;
//        bestBallPoint = Vector3.zero;
//        bestIsAerial = false;
//        bestBallTime = float.MaxValue;
//        bestMargin = float.NegativeInfinity;

//        if (fielder == null)
//            return false;

//        float runSpeed = Mathf.Max(fielder.RunSpeed, 0.1f);
//        float height = 1.8f;

//        Renderer[] renderers = fielder.GetComponentsInChildren<Renderer>();
//        if (renderers.Length > 0)
//        {
//            Bounds bounds = renderers[0].bounds;
//            foreach (Renderer renderer in renderers)
//            {
//                if (renderer != renderers[0])
//                    bounds.Encapsulate(renderer.bounds);
//            }
//            height = Mathf.Max(bounds.size.y, 0.5f);
//        }

//        float minAerialCatchHeight = height * aerialMinHeightRatio;
//        float maxCatchHeight = height * aerialMaxHeightRatio;
//        // An aerial catch should never have a tighter reach than a ground pickup.
//        float aerialReach = Mathf.Max(height * aerialReachRatio, groundCollectionReach);

//        float playableRadius = Mathf.Max(0f, groundRadius - 0.5f);

//        Vector2 center = new Vector2(groundCenter.x, groundCenter.z);
//        Vector2 fielderXZ = new Vector2(fielder.transform.position.x, fielder.transform.position.z);

//        Vector2 stumpsXZ = stumps != null
//            ? new Vector2(stumps.position.x, stumps.position.z)
//            : fielderXZ;
//        float keeperRadius = fieldingRange * 2f;
//        float keeperRadiusSqr = keeperRadius * keeperRadius;

//        bool found = false;
//        float bestScore = float.NegativeInfinity;

//        foreach (TrajectoryPoint point in trajectory)
//        {
//            if (point.time < minimumBallTime)
//                continue;

//            Vector2 pointXZ = new Vector2(point.position.x, point.position.z);

//            if ((pointXZ - center).sqrMagnitude > playableRadius * playableRadius)
//                continue;

//            float heightAboveGround = point.position.y - groundY;

//            // Too high to reach at all
//            if (heightAboveGround > maxCatchHeight)
//                continue;

//            // Keeper is limited to a circular zone around the stumps
//            if (keeperMode && (pointXZ - stumpsXZ).sqrMagnitude > keeperRadiusSqr)
//                continue;

//            bool aerial = heightAboveGround > 0.05f;
//            float reach;

//            if (!aerial)
//                reach = groundCollectionReach;
//            else if (heightAboveGround < minAerialCatchHeight)
//                reach = groundCollectionReach; // low ball – can still field it
//            else
//                reach = aerialReach;

//            float horizontalDistance = Vector2.Distance(fielderXZ, pointXZ);
//            float distanceToRun = Mathf.Max(0f, horizontalDistance - reach);
//            float arrivalTime = distanceToRun / runSpeed + fielderReactionTime;
//            float margin = point.time - arrivalTime;

//            if (margin < 0f)
//                continue;

//            float score = -point.time + margin * 0.3f;

//            if (!found || score > bestScore)
//            {
//                found = true;
//                bestScore = score;
//                bestBallPoint = point.position;
//                bestTarget = new Vector3(point.position.x, groundY, point.position.z);
//                bestIsAerial = aerial;
//                bestBallTime = point.time;
//                bestMargin = margin;
//            }
//        }

//        return found;
//    }

//    void AssignFielder(Fielder fielder, Vector3 target, Vector3 ballPoint, bool isAerial)
//    {
//        bestFielders.Add(fielder);
//        fielder.targetPosition = target;
//        fielder.interceptionPoint = ballPoint;
//        fielder.isAerialInterception = isAerial;
//        fielder.useAssignedTarget = true;
//        fielder.enabled = true;
//        fielder.startedRun = true;
//        fielder.Initiate(ball);
//    }

//    List<TrajectoryPoint> PredictTrajectory(Vector3 position, Vector3 velocity, float drag, float groundY)
//    {
//        List<TrajectoryPoint> points = new List<TrajectoryPoint>();

//        float timeStep = Time.fixedDeltaTime;
//        float elapsed = 0f;
//        bool landed = position.y <= groundY;

//        if (landed)
//            position.y = groundY;

//        for (int i = 0; i < 500; i++)
//        {
//            Vector3 previousPosition = position;

//            if (!landed)
//            {
//                velocity += Physics.gravity * timeStep;
//                velocity /= 1f + drag * timeStep;
//                position += velocity * timeStep;

//                if (position.y <= groundY)
//                {
//                    float denominator = previousPosition.y - position.y;
//                    float fraction = denominator > 0.0001f
//                        ? Mathf.Clamp01((previousPosition.y - groundY) / denominator)
//                        : 1f;

//                    position = Vector3.Lerp(previousPosition, position, fraction);
//                    position.y = groundY;
//                    elapsed += timeStep * fraction;
//                    landed = true;
//                }
//                else
//                {
//                    elapsed += timeStep;
//                }
//            }
//            else
//            {
//                velocity.y = 0f;
//                velocity /= 1f + drag * timeStep;
//                position += velocity * timeStep;
//                position.y = groundY;
//                elapsed += timeStep;
//            }

//            points.Add(new TrajectoryPoint(position, elapsed));

//            if (landed && new Vector2(velocity.x, velocity.z).sqrMagnitude < 0.01f)
//                break;
//        }

//        if (points.Count > 0)
//        {
//            Debug.Log($"TRAJECTORY PREDICTION | Points: {points.Count} | Duration: {elapsed:F2}s | Start: {points[0].position} | End: {points[points.Count - 1].position} | Final Horizontal Speed: {new Vector2(velocity.x, velocity.z).magnitude:F2} m/s");
//        }

//        return points;
//    }

//    float GetGroundY()
//    {
//        foreach (Fielder fielder in fielders)
//        {
//            if (fielder != null && fielder != keeper && !fielder.isKeeper)
//                return fielder.transform.position.y;
//        }

//        return keeper != null ? keeper.transform.position.y : 0f;
//    }

//    // ---------- Keeper-run coroutine control ----------

//    void StartKeeperRun()
//    {
//        StopKeeperRun();
//        keeperRunCoroutine = StartCoroutine(KeeperRunToRecieve());
//    }

//    void StopKeeperRun()
//    {
//        if (keeperRunCoroutine != null)
//        {
//            StopCoroutine(keeperRunCoroutine);
//            keeperRunCoroutine = null;
//        }
//    }

//    IEnumerator KeeperRunToRecieve()
//    {
//        if (keeper == null || stumps == null || ball == null)
//            yield break;

//        keeper.StopAllCoroutines();
//        keeper.enabled = true;
//        keeper.ball = ball;
//        keeper.animator.Play("running");

//        Vector3 stumpsXZ = new Vector3(stumps.position.x, keeper.transform.position.y, stumps.position.z);

//        while (Vector2.Distance(
//            new Vector2(keeper.transform.position.x, keeper.transform.position.z),
//            new Vector2(stumps.position.x, stumps.position.z)) > 2f)
//        {
//            Vector3 moveDirection = ball.position - keeper.transform.position;
//            moveDirection.y = 0f;

//            if (moveDirection.sqrMagnitude > 0.0001f)
//            {
//                Quaternion lookRotation = Quaternion.LookRotation(moveDirection.normalized);
//                keeper.transform.rotation = Quaternion.Slerp(
//                    keeper.transform.rotation,
//                    lookRotation,
//                    Time.deltaTime * 10f);
//            }

//            keeper.transform.position = Vector3.MoveTowards(
//                keeper.transform.position,
//                stumpsXZ,
//                4f * Time.deltaTime);

//            yield return null;
//        }

//        keeper.animator.Play("idle");

//        Vector3 finalDir = ball.position - keeper.transform.position;
//        finalDir.y = 0f;
//        if (finalDir.sqrMagnitude > 0.0001f)
//        {
//            keeper.transform.rotation = Quaternion.LookRotation(finalDir.normalized);
//        }
//    }

//    public void StopOtherFiedlers(Fielder fielded)
//    {
//        foreach (var fielder in bestFielders)
//        {
//            if (fielder == fielded) continue;
//            fielder.StopAll();
//        }
//    }

//    public void StopFielders()
//    {
//        foreach (Fielder fielder in bestFielders)
//        {
//            fielder.StopField();
//        }
//    }

//    public void ResetFielders()
//    {
//        if (keeper != null)
//        {
//            keeper.KeeperReset();
//            keeper.startedRun = false;
//        }

//        foreach (var fielder in bestFielders)
//        {
//            if (fielder == keeper) continue; // keeper already handled above
//            fielder.Reset();
//            fielder.startedRun = false;
//        }

//        bestFielders.Clear();
//    }
//}


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldManager : MonoBehaviour
{
    public List<Fielder> fielders;
    public List<Fielder> bestFielders = new List<Fielder>();

    public bool tryingPickup, ballWasAirBorne;
    public float fieldingRange = 1.5f;
    public float score;

    public static Vector3 hitBallPos, hitVelocity;
    public Transform ball, marker, stumps;
    public Fielder keeper;

    [SerializeField] float fielderReactionTime = 0.12f;
    [SerializeField] float groundCollectionReach = 0.45f;
    [SerializeField] float aerialMinHeightRatio = 0.3f;
    [SerializeField] float aerialMaxHeightRatio = 1.15f;
    [SerializeField] float aerialReachRatio = 0.15f;
    [SerializeField] float groundFrictionMultiplier = 5.0f; // NEW: Simulates grass friction

    [SerializeField] MeshRenderer ignoreBounds;
    private Coroutine keeperRunCoroutine;

    public static Action<Fielder> StopOther;
    public static Action<Vector3> StartCheckField;
    public static Action ResetFielder, StopField;
    public static Action<Vector3, Transform, bool> KeeperCollect;

    private void OnEnable()
    {
        StartCheckField += AssignBestFielders;
        ResetFielder += ResetFielders;
        StopField += StopFielders;
        StopOther += StopOtherFiedlers;
        KeeperCollect += KeeperCollection;
    }

    private void OnDisable()
    {
        StartCheckField -= AssignBestFielders;
        ResetFielder -= ResetFielders;
        StopField -= StopFielders;
        StopOther -= StopOtherFiedlers;
        KeeperCollect -= KeeperCollection;
    }

    void KeeperCollection(Vector3 catchPoint, Transform ball, bool edge)
    {
        keeper.KeeperRecieve(catchPoint, ball, edge);
    }

    public struct TrajectoryPoint
    {
        public Vector3 position;
        public float time;
        public Vector3 velocity;

        public TrajectoryPoint(Vector3 position, float time, Vector3 velocity)
        {
            this.position = position;
            this.time = time;
            this.velocity = velocity;
        }
    }

    public void AssignBestFielders(Vector3 ballAt)
    {
        bestFielders.Clear();
        ball = Gameplay.instance.currentBall;
        hitBallPos = ballAt;

        if (ball == null) return;
        StartCoroutine(AssignBestFieldersAfterPhysics());
    }

    IEnumerator AssignBestFieldersAfterPhysics()
    {
        yield return new WaitForFixedUpdate();

        if (ball == null || keeper == null) yield break;

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        hitVelocity = ballRb.linearVelocity;
        float groundY = GetGroundY();

        List<TrajectoryPoint> trajectory = PredictTrajectory(ball.position, ballRb.linearVelocity, ballRb.linearDamping, groundY);
        if (trajectory.Count == 0) yield break;

        Vector3 groundCenter = Gameplay.instance.stadiumBounds.center;
        float groundRadius = Mathf.Min(Gameplay.instance.stadiumBounds.extents.x, Gameplay.instance.stadiumBounds.extents.z);

        Fielder primary = null;
        Vector3 primaryTarget = Vector3.zero;
        Vector3 primaryBallPoint = Vector3.zero;
        float primaryTime = float.MaxValue;
        bool primaryIsAerial = false;
        bool keeperSelected = false;

        // Evaluate Outfielders
        foreach (Fielder fielder in fielders)
        {
            if (fielder == null || fielder == keeper || fielder.isKeeper || fielder.RunSpeed < 1.0f) continue;

            if (EvaluateFielder(fielder, trajectory, groundCenter, groundRadius, groundY, false, out Vector3 target, out Vector3 ballPoint, out bool isAerial, out float interceptTime))
            {
                if (interceptTime < primaryTime)
                {
                    primary = fielder;
                    primaryTarget = target;
                    primaryBallPoint = ballPoint;
                    primaryIsAerial = isAerial;
                    primaryTime = interceptTime;
                    keeperSelected = false;
                }
            }
        }

        // Evaluate Keeper
        if (EvaluateFielder(keeper, trajectory, groundCenter, groundRadius, groundY, true, out Vector3 kTarget, out Vector3 kBallPoint, out bool kIsAerial, out float kInterceptTime))
        {
            if (kInterceptTime < primaryTime)
            {
                primary = keeper;
                primaryTarget = kTarget;
                primaryBallPoint = kBallPoint;
                primaryIsAerial = kIsAerial;
                primaryTime = kInterceptTime;
                keeperSelected = true;
            }
        }

        if (primary == null)
        {
            StartKeeperRun();
            yield break;
        }

        if (keeperSelected)
        {
            StopKeeperRun();
            keeper.StopAllCoroutines();
            bestFielders.Add(keeper);
            keeper.interceptionPoint = primaryBallPoint;
            keeper.isAerialInterception = primaryIsAerial;
            keeper.startedRun = true;
            keeper.KeeperRecieve(primaryBallPoint, ball, true);
        }
        else
        {
            AssignFielder(primary, primaryTarget, primaryBallPoint, primaryIsAerial);
        }

        // Assign Backup (Find next best time that isn't the primary)
        Fielder backup = null;
        float backupTime = float.MaxValue;
        Vector3 backupTarget = Vector3.zero;

        foreach (Fielder fielder in fielders)
        {
            if (fielder == null || fielder == keeper || fielder.isKeeper || fielder == primary) continue;

            if (EvaluateFielder(fielder, trajectory, groundCenter, groundRadius, groundY, false, out Vector3 tTarget, out Vector3 tBallPoint, out bool tIsAerial, out float tTime))
            {
                if (tTime < backupTime)
                {
                    backup = fielder;
                    backupTarget = tTarget;
                    backupTime = tTime;
                }
            }
        }

        if (backup != null)
        {
            AssignFielder(backup, backupTarget, backupTarget, false); // Backup goes to intercept area
        }

        if (!keeperSelected) StartKeeperRun();
    }

    bool EvaluateFielder(Fielder fielder, List<TrajectoryPoint> trajectory, Vector3 groundCenter, float groundRadius, float groundY, bool keeperMode, out Vector3 bestTarget, out Vector3 bestBallPoint, out bool bestIsAerial, out float earliestInterceptTime)
    {
        bestTarget = Vector3.zero;
        bestBallPoint = Vector3.zero;
        bestIsAerial = false;
        earliestInterceptTime = float.MaxValue;

        float runSpeed = fielder.RunSpeed;
        Vector2 fielderXZ = new Vector2(fielder.transform.position.x, fielder.transform.position.z);

        Vector2 stumpsXZ = stumps != null ? new Vector2(stumps.position.x, stumps.position.z) : fielderXZ;
        float keeperRadiusSqr = (fieldingRange * 2f) * (fieldingRange * 2f);

        bool foundIntercept = false;

        // 1. Try to cut off the ball (interception)
        foreach (TrajectoryPoint point in trajectory)
        {
            Vector2 pointXZ = new Vector2(point.position.x, point.position.z);
            if (keeperMode && (pointXZ - stumpsXZ).sqrMagnitude > keeperRadiusSqr) continue;

            float horizontalDistance = Vector2.Distance(fielderXZ, pointXZ);
            float reach = (point.position.y - groundY > 0.05f) ? aerialReachRatio : groundCollectionReach;
            float timeToReach = Mathf.Max(0f, horizontalDistance - reach) / runSpeed + fielderReactionTime;

            if (timeToReach <= point.time) // Fielder can beat the ball here!
            {
                earliestInterceptTime = timeToReach;
                bestBallPoint = point.position;
                bestTarget = new Vector3(point.position.x, groundY, point.position.z);
                bestIsAerial = (point.position.y - groundY > 0.05f);
                foundIntercept = true;
                break; // Earliest point found
            }
        }

        // 2. If cutoff is impossible, calculate CHASE TIME (catch it after it passes or stops)
        if (!foundIntercept && !keeperMode)
        {
            TrajectoryPoint lastPoint = trajectory[trajectory.Count - 1];
            Vector2 lastPointXZ = new Vector2(lastPoint.position.x, lastPoint.position.z);
            float distToEnd = Vector2.Distance(fielderXZ, lastPointXZ);

            earliestInterceptTime = (distToEnd / runSpeed) + fielderReactionTime;
            bestBallPoint = lastPoint.position;
            bestTarget = new Vector3(lastPoint.position.x, groundY, lastPoint.position.z);
            bestIsAerial = false;
            return true;
        }

        return foundIntercept;
    }

    void AssignFielder(Fielder fielder, Vector3 target, Vector3 ballPoint, bool isAerial)
    {
        bestFielders.Add(fielder);
        fielder.targetPosition = target;
        fielder.interceptionPoint = ballPoint;
        fielder.isAerialInterception = isAerial;
        fielder.useAssignedTarget = true;
        fielder.enabled = true;
        fielder.startedRun = true;
        fielder.Initiate(ball);
    }

    // FIXED: Now properly applies heavy rolling friction so predictions stop at the boundary correctly.
    List<TrajectoryPoint> PredictTrajectory(Vector3 position, Vector3 velocity, float drag, float groundY)
    {
        List<TrajectoryPoint> points = new List<TrajectoryPoint>();
        float timeStep = Time.fixedDeltaTime;
        float elapsed = 0f;
        bool landed = position.y <= groundY;

        if (landed) position.y = groundY;

        for (int i = 0; i < 500; i++)
        {
            Vector3 previousPosition = position;

            if (!landed)
            {
                velocity += Physics.gravity * timeStep;
                velocity /= 1f + drag * timeStep;
                position += velocity * timeStep;

                if (position.y <= groundY)
                {
                    position.y = groundY;
                    landed = true;
                }
            }
            else
            {
                velocity.y = 0f;
                // Grass friction is much higher than air drag
                velocity /= 1f + (drag * groundFrictionMultiplier) * timeStep;
                position += velocity * timeStep;
                position.y = groundY;
            }

            elapsed += timeStep;
            points.Add(new TrajectoryPoint(position, elapsed, velocity));

            if (landed && velocity.sqrMagnitude < 0.1f) break; // Ball stopped
        }

        return points;
    }

    float GetGroundY()
    {
        foreach (Fielder fielder in fielders)
        {
            if (fielder != null && fielder != keeper && !fielder.isKeeper)
                return fielder.transform.position.y;
        }
        return keeper != null ? keeper.transform.position.y : 0f;
    }

    void StartKeeperRun()
    {
        StopKeeperRun();
        keeperRunCoroutine = StartCoroutine(KeeperRunToRecieve());
    }

    void StopKeeperRun()
    {
        if (keeperRunCoroutine != null)
        {
            StopCoroutine(keeperRunCoroutine);
            keeperRunCoroutine = null;
        }
    }

    IEnumerator KeeperRunToRecieve()
    {
        if (keeper == null || stumps == null || ball == null) yield break;

        keeper.StopAllCoroutines();
        keeper.enabled = true;
        keeper.ball = ball;
        keeper.animator.Play("running");

        Vector3 stumpsXZ = new Vector3(stumps.position.x, keeper.transform.position.y, stumps.position.z);

        while (Vector2.Distance(new Vector2(keeper.transform.position.x, keeper.transform.position.z), new Vector2(stumps.position.x, stumps.position.z)) > 2f)
        {
            keeper.transform.position = Vector3.MoveTowards(keeper.transform.position, stumpsXZ, 4f * Time.deltaTime);
            keeper.transform.LookAt(new Vector3(ball.position.x, keeper.transform.position.y, ball.position.z));
            yield return null;
        }
        keeper.animator.Play("idle");
    }

    public void StopOtherFiedlers(Fielder fielded)
    {
        foreach (var fielder in bestFielders) { if (fielder != fielded) fielder.StopAll(); }
    }

    public void StopFielders()
    {
        foreach (Fielder fielder in bestFielders) { fielder.StopField(); }
    }

    public void ResetFielders()
    {
        if (keeper != null) { keeper.KeeperReset(); keeper.startedRun = false; }
        foreach (var fielder in bestFielders)
        {
            if (fielder == keeper) continue;
            fielder.Reset();
            fielder.startedRun = false;
        }
        bestFielders.Clear();
    }
}