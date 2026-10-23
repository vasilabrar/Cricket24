//using System.Collections;
//using UnityEngine;
//using RootMotion.FinalIK;

//public class Fielder : MonoBehaviour
//{
//    [SerializeField] float runSpeed;
//    [SerializeField] float groundY = .001f;
//    [SerializeField] float distanceToTarget;
//    [SerializeField] float handReachDuration = 0.52362f;
//    [SerializeField] float handReachRadius = 0.0038362f;

//    public Vector3 interceptionPoint;
//    public bool isAerialInterception;

//    public float RunSpeed => runSpeed;

//    [SerializeField] bool chaseMode, isRightHanded,  throwable;
//    public bool isKeeper;
//    public bool startedRun;
//    public bool useAssignedTarget;

//    private Vector3 actualPos, actualRot;
//    public Vector3 targetPosition;

//    [SerializeField] GameObject rayTestObject;
//    public Transform ball, throwingArm, fielderTargetMark;

//    public Animator animator;
//    BallHit ballComp;
//    Rigidbody ballRb;

//    [SerializeField] FieldManager fm;
//    [SerializeField] SmoothInteractionPickup pickScript;

//    Coroutine keeperStrafeRoutine;

//    public AnimationClip idleClip, runningClip, jumpClip, crouchClip, moveRightClip, moveLeftClip, diveRightClip, diveLeftClip, chasePickupClip, pickUpClip, throwClip, kneelClip;

//    private void OnEnable()
//    {
//        actualPos = transform.position;
//        actualRot = transform.rotation.eulerAngles;
//        animator = GetComponent<Animator>();
//        pickScript = this.GetComponent<SmoothInteractionPickup>();
//        groundY = transform.position.y;
//    }

//    #region keeper

//    public void KeeperRecieve(Vector3 targetPosition, Transform ball, bool isEdge = false)
//    {
//        StartCoroutine(SetTarget(targetPosition, ball, isEdge));
//    }

//    IEnumerator SetTarget(Vector3 targetPosition, Transform ball, bool isEdge = false)
//    {
//        Debug.Log($"KEEPER SetTarget CALLED | Target: {targetPosition} | Current: {transform.position} | IsHolding: {pickScript.IsHolding}");
//        ballComp = ball.GetComponent<BallHit>();
//        this.ball = ball;
//        ballRb = ball.GetComponent<Rigidbody>();
//        if (targetPosition == Vector3.zero)
//        {
//            Vector3 offset = ball.position - transform.position;
//            offset.y = 0;
//            while (offset.magnitude > GetCollectionStartDistance())
//            {
//                yield return null;
//                offset = ball.position - transform.position;
//                offset.y = 0;
//            }
//            pickScript.pickupObject = ball.GetComponent<InteractionObject>();
//            pickScript.StartPickup(false, this);
//            yield break;
//        }

//        else
//        {
//            keeperStrafeRoutine = StartCoroutine(StrafeWhenNeeded(targetPosition));

//            if (targetPosition.y > 2f)
//            {
//                Debug.Log("jump..");
//            }
//            else if (targetPosition.y < .7f)
//            {
//                Debug.Log("croutch..");
//            }
//        }


//        if (!ballComp.secondTouch)
//        {
//            fm.marker.position = targetPosition;
//        }


//        //Debug.Log("wait recive");
//        yield return new WaitUntil(() => CanStartPickup());

//        //while (keeperStrafeRoutine != null)
//        //    yield return null;


//        //Debug.Log("recive start");

//        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
//        Debug.Log($"KEEPER PICKUP START | Current: {transform.position} | Target: {targetPosition} | Strafe Running: {keeperStrafeRoutine != null}");
//        pickScript.StartKeeperPickup(false, this, targetPosition);

//    }


//    //IEnumerator StrafeWhenNeeded(Vector3 targetPosition)
//    //{
//    //    //Debug.Log($"KEEPER STRAFE START | Target X: {targetPosition.x:F2} | Current X: {transform.position.x:F2} | Delta: {targetPosition.x - transform.position.x:F2}");
//    //    while (Mathf.Abs(targetPosition.x - transform.position.x) > 0.15f)
//    //    {
//    //        float direction = Mathf.Sign(targetPosition.x - transform.position.x);

//    //        if (Mathf.Abs(direction) < 0.01f)
//    //            break;

//    //        bool stepComplete = pickScript.StrafeStep(direction);
//    //        Debug.Log($"KEEPER STRAFE | X: {transform.position.x:F2} | Target X: {targetPosition.x:F2} | Direction: {direction} | Step Complete: {stepComplete}");

//    //        if (stepComplete)
//    //            //Debug.Log($"Keeper shuffle step completed | Current X: {transform.position.x:F2}");

//    //        yield return null;
//    //    }

//    //    //pickScript.CancelStrafe();
//    //    keeperStrafeRoutine = null;

//    //    //Debug.Log("Keeper strafe stopped");
//    //}


//    IEnumerator StrafeWhenNeeded(Vector3 targetPosition)
//    {
//        float targetX = targetPosition.x;

//        while (Mathf.Abs(targetX - transform.position.x) > 0.15f)
//        {
//            float direction = Mathf.Sign(targetX - transform.position.x);

//            if (Mathf.Abs(direction) < 0.01f)
//                break;

//            pickScript.StrafeStep(direction);

//            yield return null;
//        }

//        // Finish the current three-phase step without moving the root farther.
//        while (pickScript.IsStrafing)
//        {
//            pickScript.StrafeStep(0f);
//            yield return null;
//        }

//        keeperStrafeRoutine = null;
//    }



//    public void PickupComplete()
//    {
//        if (keeperStrafeRoutine != null)
//        {
//            StopCoroutine(keeperStrafeRoutine);
//            keeperStrafeRoutine = null;
//        }

//        pickScript.CancelStrafe();
//        if (isKeeper)
//        {
//            if(ballComp.secondTouch)
//            {
//                Gameplay.instance.deliveryDead = true;
//            }
//            else
//            {
//                Gameplay.instance.deliveryDead = true;
//            }
//        }
//        else
//        {
//            StartCoroutine(FielderPickupThrow());
//        }
//    }

//    #endregion

//    public void Initiate(Transform ball)
//    {
//        pickScript.interactionSystem.enabled = true;
//        StartCoroutine(StartField(ball));
//    }

//    //IEnumerator StartField(Transform ball)
//    //{
//    //    yield return new WaitForSeconds(0.2f);

//    //    targetPosition.y = groundY;
//    //    actualPos = transform.position;
//    //    actualRot = transform.rotation.eulerAngles;
//    //    ballComp = ball.GetComponent<BallHit>();
//    //    ballRb = ball.GetComponent<Rigidbody>();
//    //    this.ball = ball;

//    //    fm.marker.position = targetPosition;

//    //    if (!ballComp.groundShot)
//    //    {
//    //        Debug.Log("airball cal");
//    //        targetPosition = PredictBallPosition(ballComp.shotPoint, ballComp.shotForce, ballRb.linearDamping);
//    //        fm.marker.position = targetPosition;
//    //        StartCoroutine(RunToBall(false));
//    //    }

//    //    else if (IsBallComingAtFielder() && !this.gameObject.CompareTag("DeepFielder") && ballComp.groundShot)
//    //    {
//    //        Debug.Log("Coming to fielder");
//    //        StartCoroutine(WaitForBall());
//    //    }

//    //    else
//    //    {
//    //        Debug.Log("away from fielder");
//    //        StartCoroutine(RunToBall(false));
//    //    }
//    //}



//    IEnumerator StartField(Transform ball)
//    {
//        if (!useAssignedTarget)
//            yield return new WaitForSeconds(0.2f);

//        targetPosition.y = groundY;
//        actualPos = transform.position;
//        actualRot = transform.rotation.eulerAngles;
//        ballComp = ball.GetComponent<BallHit>();
//        ballRb = ball.GetComponent<Rigidbody>();
//        this.ball = ball;

//        fm.marker.position = targetPosition;

//        if (!useAssignedTarget && !ballComp.groundShot)
//        {
//            Debug.Log("airball cal");
//            targetPosition = PredictBallPosition(ballComp.shotPoint, ballComp.shotForce, ballRb.linearDamping);
//            fm.marker.position = targetPosition;
//            StartCoroutine(RunToBall(false));
//        }

//        else if (!useAssignedTarget && IsBallComingAtFielder() && !this.gameObject.CompareTag("DeepFielder") && ballComp.groundShot)
//        {
//            Debug.Log("Coming to fielder");
//            StartCoroutine(WaitForBall());
//        }

//        else
//        {
//            Debug.Log("Running to assigned or predicted target");
//            StartCoroutine(RunToBall(false));
//        }
//    }



//    void ComputeTarget(bool restart)
//    {
//        if (restart)
//        {
//            targetPosition = ball.position;
//        }
//        else if (chaseMode)
//        {
//            Vector3 ballVelocity = ballRb.linearVelocity;
//            Vector3 moveDir = new Vector3(ballVelocity.x, 0f, ballVelocity.z);

//            if (moveDir.magnitude < 0.1f)
//            {
//                targetPosition = ball.position;
//            }
//            else
//            {
//                moveDir.Normalize();

//                Vector3 toFielder = transform.position - ball.position;
//                toFielder.y = 0f;
//                float sqrMag = toFielder.sqrMagnitude;

//                bool fielderAhead = false;
//                if (sqrMag > 0.01f)
//                {
//                    fielderAhead = Vector3.Dot(toFielder / Mathf.Sqrt(sqrMag), moveDir) > 0.3f;
//                }

//                if (fielderAhead)
//                {
//                    // Do NOT lead — go directly to ball
//                    targetPosition = ball.position;
//                }
//                else
//                {
//                    // Scale lead distance with ball velocity
//                    float leadDistance = Mathf.Clamp(ballVelocity.magnitude * 0.3f, 1.5f, 5f);

//                    Vector3 lateralDir = Vector3.Cross(Vector3.up, moveDir).normalized;
//                    Vector3 sideOffset = (isRightHanded ? lateralDir : -lateralDir) * 1.2f;

//                    targetPosition = ball.position + (moveDir * leadDistance) + sideOffset;
//                }
//            }

//            targetPosition.y = groundY;
//        }
//    }

//    float GetCollectionStartDistance()
//    {
//        float ballSpeed = ballRb.linearVelocity.magnitude;
//        float reqDistance = ballSpeed * handReachDuration;
//        return Mathf.Max(reqDistance, handReachRadius);
//    }

//    IEnumerator RunToBall(bool restart)
//    {
//        if (restart) Debug.Log("second time");
//        animator.Play("running");
//        float distToBall = 0;
//        while (!ballComp.stopTriggered)
//        {
//            Vector3 offsetBall = ball.position - transform.position;
//            offsetBall.y = 0;
//            distToBall = offsetBall.magnitude;

//            Vector3 offsetTarget = targetPosition - transform.position;
//            offsetTarget.y = 0;
//            distanceToTarget = offsetTarget.magnitude;

//            if (Gameplay.instance.deliveryDead)
//            {
//                StopAll();
//                yield break;
//            }

//            if (!ballComp.groundShot)
//            {
//                if (CanStartPickup())
//                {
//                    Debug.Log("air ball reached taret");
//                    StartCoroutine(GrabBall());
//                    yield break;
//                }
//            }
//            else
//            {
//                if (!useAssignedTarget)
//                    ComputeTarget(restart);

//                if (ShouldChase(ball, transform.position) && !chaseMode)
//                {
//                    Debug.Log(gameObject.name + " has to chase");
//                    chaseMode = true;
//                }

//                if (distanceToTarget <= 2f)
//                {
//                    if (!chaseMode && IsBallComingAtFielder() && ballRb.linearVelocity.magnitude > 10)
//                    {
//                        StartCoroutine(WaitForBall());
//                        yield break;
//                    }
//                    else
//                    {

//                        if (!chaseMode)
//                            restart = true;
//                    }
//                }

//                if (chaseMode)
//                {
//                    if (CanStartPickup())
//                    {
//                        if (fm.tryingPickup)
//                        {
//                            while (fm.tryingPickup)
//                            {
//                                if (ballComp.stopTriggered)
//                                {
//                                    yield break;
//                                }
//                                yield return null;
//                            }
//                        }
//                        Debug.Log("Ball in hand range for chase- Picking up " + distToBall);
//                        ballComp.fieldedPlayer = this.gameObject;
//                        fm.marker.position = ball.position;
//                        animator.Play("idle");
//                        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
//                        pickScript.StartPickup(false, this);
//                        fm.tryingPickup = true;
//                        yield break;
//                    }
//                }
//                else
//                {
//                    if (CanStartPickup())
//                    {
//                        if (fm.tryingPickup)
//                        {
//                            while (fm.tryingPickup)
//                            {
//                                if (ballComp.stopTriggered)
//                                {
//                                    yield break;
//                                }
//                                yield return null;
//                            }
//                        }
//                        Debug.Log("Ball in hand range - Picking up " + distToBall);
//                        ballComp.fieldedPlayer = this.gameObject;
//                        fm.marker.transform.position = ball.position;
//                        animator.Play("idle");
//                        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
//                        pickScript.StartPickup(false, this);
//                        yield break;
//                    }

//                }
//            }
//            Vector3 moveDirection = (targetPosition - transform.position).normalized;
//            if (moveDirection.sqrMagnitude > 0.01f)
//            {
//                Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
//                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
//            }
//            transform.position = Vector3.MoveTowards(transform.position, targetPosition, runSpeed * Time.deltaTime);
//            yield return null;
//        }
//    }

//    IEnumerator WaitForBall()
//    {
//        Debug.Log(gameObject.name + " Enter WaitForBall");
//        animator.Play("idle");

//        while (!ballComp.fielderReached)
//        {
//            Vector3 offsetBall = ball.position - transform.position;
//            offsetBall.y = 0;
//            float distToBall = offsetBall.magnitude;

//            Vector3 lookDir = (ball.position - transform.position).normalized;
//            lookDir.y = 0;
//            if (lookDir != Vector3.zero)
//            {
//                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);
//            }

//            if (CanStartPickup())
//            {
//                if (fm.tryingPickup)
//                {
//                    while (fm.tryingPickup)
//                    {
//                        if (ballComp.stopTriggered)
//                        {
//                            yield break;
//                        }
//                        yield return null;
//                    }
//                }
//                fm.marker.position = ball.position;
//                pickScript.pickupObject = ball.GetComponent<InteractionObject>();
//                pickScript.StartPickup(false, this);
//                yield break;
//            }

//            if (ballRb.linearVelocity.magnitude < 5.0f)
//            {
//                Debug.Log("restart runn");
//                StartCoroutine(RunToBall(true));
//                yield break;
//            }
//            yield return null;
//        }
//        animator.Play("idle");
//        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
//        if (fm.tryingPickup)
//        {
//            while (fm.tryingPickup)
//            {
//                if (ballComp.stopTriggered)
//                {
//                    yield break;
//                }
//                yield return null;
//            }
//        }
//        pickScript.StartPickup(false, this);
//    }


//    private bool CanStartPickup()
//    {
//        if (ball == null || ballRb == null)
//            return false;

//        if (isKeeper)
//        {
//            //Debug.Log("collect? "+CanKeeperStartPickup());
//            return CanKeeperStartPickup();
//        }

//        Vector3 ballPos = ball.position;
//        Vector3 fielderPos = transform.position;

//        ballPos.y = 0f;
//        fielderPos.y = 0f;

//        Vector3 ballVel = ballRb.linearVelocity;
//        ballVel.y = 0f;

//        Vector3 fielderVel = (targetPosition - transform.position).normalized * runSpeed;
//        fielderVel.y = 0f;

//        Vector3 relPos = ballPos - fielderPos;
//        Vector3 relVel = ballVel - fielderVel;

//        float relSpeedSq = relVel.sqrMagnitude;

//        if (relSpeedSq < 0.01f)
//            return false;

//        float t = -Vector3.Dot(relPos, relVel) / relSpeedSq;

//        if (t < 0f || t > handReachDuration)
//            return false;

//        Vector3 closest = relPos + relVel * t;

//        return closest.magnitude <= handReachRadius;
//    }


//    private bool CanKeeperStartPickup()
//    {
//        float timeToPlane = GetBallTimeToZ(25f);

//        return timeToPlane >= 0f && timeToPlane <= pickScript.keeperReachDuration;
//    }


//    private float GetBallTimeToZ(float targetZ)
//    {
//        Vector3 position = ball.position;
//        Vector3 velocity = ballRb.linearVelocity;

//        float timeStep = Time.fixedDeltaTime;
//        float elapsed = 0f;
//        float drag = ballRb.linearDamping;

//        for (int i = 0; i < 500; i++)
//        {
//            Vector3 previousPosition = position;

//            velocity += Physics.gravity * timeStep;
//            velocity /= 1f + drag * timeStep;
//            position += velocity * timeStep;
//            elapsed += timeStep;

//            if ((previousPosition.z - targetZ) * (position.z - targetZ) <= 0f &&
//                previousPosition.z != position.z)
//            {
//                float fraction = (targetZ - previousPosition.z) /
//                                 (position.z - previousPosition.z);

//                return elapsed - timeStep + fraction * timeStep;
//            }

//            if (velocity.sqrMagnitude < 0.01f)
//                break;
//        }

//        return -1f;
//    }

//    public void StopAll()
//    {
//        animator.Play("idle");
//    }

//    IEnumerator GrabBall()
//    {
//        animator.Play("idle");
//        while (Vector3.Distance(ball.position, transform.position) > GetCollectionStartDistance())
//        {
//            yield return null;
//        }
//        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
//        if (fm.tryingPickup)
//        {
//            while (fm.tryingPickup)
//            {
//                if (ballComp.stopTriggered)
//                {
//                    yield break;
//                }
//                yield return null;
//            }
//        }
//        pickScript.StartPickup(false, this);
//    }

//    IEnumerator FielderPickupThrow()
//    {
//        if (gameObject.name == "keeper")
//        {
//            Debug.Log("fld done");
//            Gameplay.instance.deliveryDead = true;
//            yield break;
//        }
//        yield return new WaitUntil(() => pickScript.isTransitioning == false);
//        ball.transform.position = throwingArm.position;
//        ball.transform.SetParent(throwingArm);
//        Vector3 lookDirection = (fm.keeper.transform.position - transform.position).normalized;
//        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
//        lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
//        transform.rotation = lookRotation;
//        yield return new WaitForSeconds(.7f);

//        animator.Play("throw");
//        yield return new WaitUntil(() => throwable);
//        pickScript.interactionSystem.enabled = false;
//        yield return new WaitForEndOfFrame();
//        Vector3 latePos = ball.position;
//        Vector3 lateScale = new Vector3(0.06116f, 0.06116f, 0.06116f);
//        ball.SetParent(null, false);
//        ball.position = latePos;
//        ball.localScale = lateScale;
//        // --- NEW LOGIC

//        Vector3 ballPosFlat = new Vector3(ball.position.x, 0, ball.position.z);
//        Vector3 keeperPosFlat = new Vector3(fm.keeper.transform.position.x, 0, fm.keeper.transform.position.z);
//        float distToKeeper = Vector3.Distance(ballPosFlat, keeperPosFlat);

//        float pitchRatio = (distToKeeper > 21f) ? .8f : 1f;

//        Vector3 dirToKeeper = (fm.keeper.transform.position - ball.position).normalized;
//        Vector3 pitchPoint = ballPosFlat + (dirToKeeper * (distToKeeper * 1));

//        if (distToKeeper <= 21f) pitchPoint -= dirToKeeper * .80f;

//        float maxArcHeight = 2.4f;
//        float gravity = Mathf.Abs(Physics.gravity.y);

//        float verticalVelocity = Mathf.Sqrt(2 * gravity * maxArcHeight);

//        float flightTime = 2.5f * (verticalVelocity / gravity);

//        float distanceToPitch = Vector3.Distance(ballPosFlat, pitchPoint);
//        float horizontalSpeed = distanceToPitch / flightTime;


//        Vector3 velocity = dirToKeeper * horizontalSpeed;
//        velocity.y = verticalVelocity;
//        Debug.DrawLine(ball.position, pitchPoint, Color.red, 15f);
//        ballRb.WakeUp();
//        ballRb.isKinematic = false;
//        ballRb.linearVelocity = velocity;

//        fm.keeper.GetComponent<Fielder>().KeeperRecieve(Vector3.zero, ball);

//        yield return new WaitForSeconds(0.5f);

//        Gameplay.instance.deliveryDead = true;

//        StopAllCoroutines();
//    }

//    #region HelperMethods

//    private bool IsBallComingAtFielder(float maxLateralDistance = 0.6f)
//    {
//        Vector3 ballPos = ball.position;
//        Vector3 fielderPos = transform.position;

//        Vector3 v = ballRb.linearVelocity;
//        v.y = 0f;

//        if (v.sqrMagnitude < 0.01f)
//            return false;

//        Vector3 toFielder = fielderPos - ballPos;
//        toFielder.y = 0f;

//        // Time of closest approach along velocity vector
//        float t = Vector3.Dot(toFielder, v) / v.sqrMagnitude;

//        // Ball is moving away or already passed
//        if (t <= 0f)
//            return false;

//        // Closest point on ball trajectory
//        Vector3 closestPoint = ballPos + v * t;

//        // Horizontal miss distance
//        float missDistance = Vector3.Distance(
//            new Vector3(closestPoint.x, 0, closestPoint.z),
//            new Vector3(fielderPos.x, 0, fielderPos.z)
//        );

//        return missDistance <= maxLateralDistance;
//    }

//    public Vector3 PredictBallPosition(Vector3 startPos, Vector3 velocity, float drag)
//    {
//        Vector3 currentPos = startPos;
//        Vector3 currentVel = velocity;

//        float timeStep = Time.fixedDeltaTime; // Always use engine value
//        Vector3 previousPos = currentPos;

//        for (int i = 0; i < 1000; i++)
//        {
//            previousPos = currentPos;

//            // Apply gravity
//            currentVel += Physics.gravity * timeStep;

//            // Apply drag (Unity Rigidbody formula)
//            currentVel *= 1f / (1f + drag * timeStep);

//            // Move
//            currentPos += currentVel * timeStep;

//            // Ground crossing check
//            if (currentPos.y <= groundY)
//            {
//                // Interpolate exact hit point
//                float t = (previousPos.y - groundY) / (previousPos.y - currentPos.y);
//                Vector3 hitPoint = Vector3.Lerp(previousPos, currentPos, t);
//                hitPoint.y = groundY;
//                return hitPoint;
//            }
//        }

//        return currentPos;
//    }

//    bool ShouldChase(Transform ball, Vector3 fielderPosition)
//    {
//        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

//        // 1. Get the ball's movement direction on the horizontal plane
//        Vector2 ballVel2D = new Vector2(ballRb.linearVelocity.x, ballRb.linearVelocity.z);

//        // If the ball isn't moving, no need to "chase" it
//        if (ballVel2D.sqrMagnitude < 0.1f) return false;

//        Vector2 ballDir = ballVel2D.normalized;

//        // 2. Vector from the ball TO the fielder
//        Vector2 ballToFielder = new Vector2(fielderPosition.x - ball.position.x,
//                                            fielderPosition.z - ball.position.z);

//        // 3. Dot Product: 
//        // If the ball's direction and the vector to the fielder are OPPOSITE, 
//        // it means the ball is moving away from the fielder (Chase mode!).
//        float dot = Vector2.Dot(ballDir, ballToFielder);

//        // If dot < 0, the fielder is "behind" the ball's current travel vector
//        return dot < 0;
//    }

//    #endregion

//    public void StopField()
//    {
//        animator.Play("idle");
//        if (isKeeper) KeeperReset();
//        else
//            Reset();
//    }

//    public void KeeperReset()
//    {
//        throwable = false;
//        chaseMode = false;
//        animator.Play("idle");
//        ball = null;
//        startedRun = false;
//        transform.position = actualPos;
//        transform.rotation = Quaternion.Euler(actualRot);
//        this.enabled = false;
//        useAssignedTarget = false;
//    }

//    public void Reset()
//    {
//        throwable = false;
//        chaseMode = false;
//        animator.Play("idle");
//        StopAllCoroutines();
//        ball = null;
//        startedRun = false;
//        transform.position = actualPos;
//        transform.rotation = Quaternion.Euler(actualRot);
//        useAssignedTarget = false;
//        this.enabled = false;
//    }
//}

using System.Collections;
using UnityEngine;
using RootMotion.FinalIK;

public class Fielder : MonoBehaviour
{
    [SerializeField] float runSpeed;
    [SerializeField] float groundY = .001f;
    [SerializeField] float distanceToTarget;
    [SerializeField] float handReachDuration = 0.52362f;
    [SerializeField] float handReachRadius = 0.0038362f;

    public Vector3 interceptionPoint;
    public bool isAerialInterception;

    public float RunSpeed => runSpeed;

    [SerializeField] bool chaseMode, isRightHanded, throwable;
    public bool isKeeper;
    public bool startedRun;
    public bool useAssignedTarget;

    private Vector3 actualPos, actualRot;
    public Vector3 targetPosition;

    [SerializeField] GameObject rayTestObject;
    public Transform ball, throwingArm, fielderTargetMark;

    public Animator animator;
    BallHit ballComp;
    Rigidbody ballRb;

    [SerializeField] FieldManager fm;
    [SerializeField] SmoothInteractionPickup pickScript;

    Coroutine keeperStrafeRoutine;

    public AnimationClip idleClip, runningClip, jumpClip, crouchClip, moveRightClip, moveLeftClip, diveRightClip, diveLeftClip, chasePickupClip, pickUpClip, throwClip, kneelClip;

    private void OnEnable()
    {
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
        animator = GetComponent<Animator>();
        pickScript = this.GetComponent<SmoothInteractionPickup>();
        groundY = transform.position.y;

        // Defensive: clear stale movement state on (re)activation.
        useAssignedTarget = false;
        chaseMode = false;
        throwable = false;
    }

    #region keeper

    public void KeeperRecieve(Vector3 targetPosition, Transform ball, bool isEdge = false)
    {
        StartCoroutine(SetTarget(targetPosition, ball, isEdge));
    }

    IEnumerator SetTarget(Vector3 targetPosition, Transform ball, bool isEdge = false)
    {
        Debug.Log($"KEEPER SetTarget CALLED | Target: {targetPosition} | Current: {transform.position} | IsHolding: {pickScript.IsHolding}");
        ballComp = ball.GetComponent<BallHit>();
        this.ball = ball;
        ballRb = ball.GetComponent<Rigidbody>();
        if (targetPosition == Vector3.zero)
        {
            Vector3 offset = ball.position - transform.position;
            offset.y = 0;
            while (offset.magnitude > GetCollectionStartDistance())
            {
                yield return null;
                offset = ball.position - transform.position;
                offset.y = 0;
            }
            pickScript.pickupObject = ball.GetComponent<InteractionObject>();
            pickScript.StartPickup(false, this);
            yield break;
        }
        else
        {
            keeperStrafeRoutine = StartCoroutine(StrafeWhenNeeded(targetPosition));

            if (targetPosition.y > 2f)
            {
                Debug.Log("jump..");
            }
            else if (targetPosition.y < .7f)
            {
                Debug.Log("croutch..");
            }
        }

        if (!ballComp.secondTouch)
        {
            fm.marker.position = targetPosition;
        }

        yield return new WaitUntil(() => CanStartPickup());

        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
        Debug.Log($"KEEPER PICKUP START | Current: {transform.position} | Target: {targetPosition} | Strafe Running: {keeperStrafeRoutine != null}");
        pickScript.StartKeeperPickup(false, this, targetPosition);
    }

    IEnumerator StrafeWhenNeeded(Vector3 targetPosition)
    {
        float targetX = targetPosition.x;

        while (Mathf.Abs(targetX - transform.position.x) > 0.15f)
        {
            float direction = Mathf.Sign(targetX - transform.position.x);

            if (Mathf.Abs(direction) < 0.01f)
                break;

            pickScript.StrafeStep(direction);

            yield return null;
        }

        // Finish the current three-phase step without moving the root farther.
        while (pickScript.IsStrafing)
        {
            pickScript.StrafeStep(0f);
            yield return null;
        }

        keeperStrafeRoutine = null;
    }

    public void PickupComplete()
    {
        if (keeperStrafeRoutine != null)
        {
            StopCoroutine(keeperStrafeRoutine);
            keeperStrafeRoutine = null;
        }

        pickScript.CancelStrafe();
        if (isKeeper)
        {
            Gameplay.instance.deliveryDead = true;
        }
        else
        {
            StartCoroutine(FielderPickupThrow());
        }
    }

    #endregion

    public void Initiate(Transform ball)
    {
        pickScript.interactionSystem.enabled = true;
        StartCoroutine(StartField(ball));
    }

    IEnumerator StartField(Transform ball)
    {
        // 1. HUMAN REACTION TIME: 
        // Delay moving by 0.2 to 0.35 seconds to simulate human cognitive processing.
        float humanReactionDelay = UnityEngine.Random.Range(0.2f, 0.35f);
        yield return new WaitForSeconds(humanReactionDelay);

        targetPosition.y = groundY;
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
        ballComp = ball.GetComponent<BallHit>();
        ballRb = ball.GetComponent<Rigidbody>();
        this.ball = ball;

        fm.marker.position = targetPosition;

        if (!useAssignedTarget && !ballComp.groundShot)
        {
            targetPosition = PredictBallPosition(ballComp.shotPoint, ballComp.shotForce, ballRb.linearDamping);
            fm.marker.position = targetPosition;
            StartCoroutine(RunToBall(false));
        }
        else if (!useAssignedTarget && IsBallComingAtFielder() && !this.gameObject.CompareTag("DeepFielder") && ballComp.groundShot)
        {
            StartCoroutine(WaitForBall());
        }
        else
        {
            StartCoroutine(RunToBall(false));
        }
    }



    float GetCollectionStartDistance()
    {
        float ballSpeed = ballRb.linearVelocity.magnitude;
        float reqDistance = ballSpeed * handReachDuration;
        return Mathf.Max(reqDistance, handReachRadius);
    }

    // Inside Fielder.cs, replace ComputeTarget and RunToBall with these:

    void ComputeTarget(bool restart)
    {
        if (restart)
        {
            targetPosition = ball.position;
            targetPosition.y = groundY;
            return;
        }

        Vector3 ballVelocity = ballRb.linearVelocity;
        Vector3 moveDir = new Vector3(ballVelocity.x, 0f, ballVelocity.z);

        if (moveDir.magnitude < 1.0f)
        {
            // Ball is slow/stopped, go straight to it
            targetPosition = ball.position;
        }
        else
        {
            // Dynamic Chase: Aim slightly ahead of the ball based on its speed
            Vector3 toFielder = transform.position - ball.position;
            toFielder.y = 0f;

            // If ball is past the fielder, chase directly to where it is + slight lead
            float leadTime = distanceToTarget / Mathf.Max(runSpeed, 1f);
            Vector3 predictedBallPos = ball.position + (moveDir * Mathf.Min(leadTime, 0.5f));
            targetPosition = predictedBallPos;
        }

        targetPosition.y = groundY;
    }

    IEnumerator RunToBall(bool restart)
    {
        animator.Play("running");
        bool isRunning = true;
        float distToBall = 0;

        while (!ballComp.stopTriggered)
        {
            if (Gameplay.instance.deliveryDead)
            {
                StopAll();
                yield break;
            }

            Vector3 offsetBall = ball.position - transform.position;
            offsetBall.y = 0;
            distToBall = offsetBall.magnitude;

            // If we are close enough and can pickup, trigger it immediately
            if (CanStartPickup())
            {
                fm.marker.transform.position = ball.position;
                animator.Play("idle");
                ballComp.fieldedPlayer = this.gameObject;
                pickScript.pickupObject = ball.GetComponent<InteractionObject>();

                if (fm.tryingPickup)
                {
                    while (fm.tryingPickup && !ballComp.stopTriggered) yield return null;
                }

                fm.tryingPickup = true;
                pickScript.StartPickup(false, this);
                yield break;
            }

            // Update Target Dynamically if the ball is on the ground
            if (ballComp.groundShot || ball.position.y <= groundY + 0.1f)
            {
                ComputeTarget(restart);
            }

            Vector3 offsetTarget = targetPosition - transform.position;
            offsetTarget.y = 0;
            distanceToTarget = offsetTarget.magnitude;

            // Smooth Movement Execution
            if (distanceToTarget > 0.1f && runSpeed > 0.01f)
            {
                if (!isRunning)
                {
                    animator.Play("running");
                    isRunning = true;
                }

                Vector3 moveDirection = (targetPosition - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, runSpeed * Time.deltaTime);
            }
            else
            {
                if (isRunning)
                {
                    animator.Play("idle");
                    isRunning = false;
                }
            }

            yield return null;
        }
    }

    IEnumerator WaitForBall()
    {
        Debug.Log(gameObject.name + " Enter WaitForBall");
        animator.Play("idle");

        while (!ballComp.fielderReached)
        {
            Vector3 offsetBall = ball.position - transform.position;
            offsetBall.y = 0;
            float distToBall = offsetBall.magnitude;

            Vector3 lookDir = (ball.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);
            }

            if (CanStartPickup())
            {
                if (fm.tryingPickup)
                {
                    while (fm.tryingPickup)
                    {
                        if (ballComp.stopTriggered)
                            yield break;
                        yield return null;
                    }
                }
                fm.marker.position = ball.position;
                pickScript.pickupObject = ball.GetComponent<InteractionObject>();
                pickScript.StartPickup(false, this);
                yield break;
            }

            if (ballRb.linearVelocity.magnitude < 5.0f)
            {
                Debug.Log("restart runn");
                StartCoroutine(RunToBall(true));
                yield break;
            }
            yield return null;
        }
        animator.Play("idle");
        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
        if (fm.tryingPickup)
        {
            while (fm.tryingPickup)
            {
                if (ballComp.stopTriggered)
                    yield break;
                yield return null;
            }
        }
        pickScript.StartPickup(false, this);
    }

    private bool CanStartPickup()
    {
        if (ball == null || ballRb == null) 
            return false;

        if (isKeeper) 
            return CanKeeperStartPickup();

        Vector3 ballPos = new Vector3(ball.position.x, 0f, ball.position.z);
        Vector3 fielderPos = new Vector3(transform.position.x, 0f, transform.position.z);
        
        float currentDistance = Vector3.Distance(ballPos, fielderPos);
        float ballSpeed = ballRb.linearVelocity.magnitude;

        // 1. HARD CAP: Ball must be physically right next to them
        float maxPhysicalReach = 1.2f; 
        if (currentDistance > maxPhysicalReach) 
            return false;

        // 2. THE "BEATEN" CHECK (Crucial for fine leg/third man racing balls)
        if (ballSpeed > 8.0f) // If the ball is moving fast (approx > 28 km/h)
        {
            Vector3 toBall = (ballPos - fielderPos).normalized;
            float forwardDot = Vector3.Dot(transform.forward, toBall);
            
            // If the ball is past their side/behind them (Dot product < 0 means it's behind their forward-facing 180 degree arc)
            if (forwardDot < 0.1f) 
            {
                Debug.Log($"{gameObject.name} was BEATEN by the pace! Must chase.");
                return false; 
            }

            // Optional: If you want them to miss balls that are too far to the side (needs a dive, but they are just standing)
            // A forwardDot of 0.8 to 1.0 means it's coming straight at their chest. 
            // If it's below 0.5, it's wide to their left/right.
            if (forwardDot < 0.6f && ballSpeed > 15.0f)
            {
                Debug.Log($"{gameObject.name} beaten laterally! Pace too high to stretch.");
                return false;
            }
        }

        // 3. Normal Relative Velocity Check for slower/reachable balls
        Vector3 ballVel = ballRb.linearVelocity;
        ballVel.y = 0f;
        
        Vector3 fielderVel = (targetPosition - transform.position).normalized * runSpeed;
        fielderVel.y = 0f;

        Vector3 relPos = ballPos - fielderPos;
        Vector3 relVel = ballVel - fielderVel;
        float relSpeedSq = relVel.sqrMagnitude;

        if (relSpeedSq < 0.01f)
            return currentDistance <= handReachRadius;

        float t = -Vector3.Dot(relPos, relVel) / relSpeedSq;
        
        // If the time to intercept is in the past, or takes longer than the animation, fail.
        if (t < 0f || t > handReachDuration) 
            return false;

        Vector3 closest = relPos + relVel * t;
        
        return closest.magnitude <= handReachRadius;
    }

    private bool CanKeeperStartPickup()
    {
        float timeToPlane = GetBallTimeToZ(25f);
        return timeToPlane >= 0f && timeToPlane <= pickScript.keeperReachDuration;
    }

    private float GetBallTimeToZ(float targetZ)
    {
        Vector3 position = ball.position;
        Vector3 velocity = ballRb.linearVelocity;

        float timeStep = Time.fixedDeltaTime;
        float elapsed = 0f;
        float drag = ballRb.linearDamping;

        for (int i = 0; i < 500; i++)
        {
            Vector3 previousPosition = position;

            velocity += Physics.gravity * timeStep;
            velocity /= 1f + drag * timeStep;
            position += velocity * timeStep;
            elapsed += timeStep;

            if ((previousPosition.z - targetZ) * (position.z - targetZ) <= 0f &&
                previousPosition.z != position.z)
            {
                float fraction = (targetZ - previousPosition.z) /
                                 (position.z - previousPosition.z);

                return elapsed - timeStep + fraction * timeStep;
            }

            if (velocity.sqrMagnitude < 0.01f)
                break;
        }

        return -1f;
    }

    public void StopAll()
    {
        StopAllCoroutines();
        chaseMode = false;
        animator.Play("idle");
    }

    IEnumerator GrabBall()
    {
        animator.Play("idle");
        while (Vector3.Distance(ball.position, transform.position) > GetCollectionStartDistance())
        {
            yield return null;
        }
        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
        if (fm.tryingPickup)
        {
            while (fm.tryingPickup)
            {
                if (ballComp.stopTriggered)
                    yield break;
                yield return null;
            }
        }
        pickScript.StartPickup(false, this);
    }

    IEnumerator FielderPickupThrow()
    {
        if (gameObject.name == "keeper")
        {
            Debug.Log("fld done");
            Gameplay.instance.deliveryDead = true;
            yield break;
        }
        yield return new WaitUntil(() => pickScript.isTransitioning == false);
        ball.transform.position = throwingArm.position;
        ball.transform.SetParent(throwingArm);
        Vector3 lookDirection = (fm.keeper.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
        transform.rotation = lookRotation;
        yield return new WaitForSeconds(.7f);

        animator.Play("throw");
        yield return new WaitUntil(() => throwable);
        pickScript.interactionSystem.enabled = false;
        yield return new WaitForEndOfFrame();
        Vector3 latePos = ball.position;
        Vector3 lateScale = new Vector3(0.06116f, 0.06116f, 0.06116f);
        ball.SetParent(null, false);
        ball.position = latePos;
        ball.localScale = lateScale;

        Vector3 ballPosFlat = new Vector3(ball.position.x, 0, ball.position.z);
        Vector3 keeperPosFlat = new Vector3(fm.keeper.transform.position.x, 0, fm.keeper.transform.position.z);
        float distToKeeper = Vector3.Distance(ballPosFlat, keeperPosFlat);

        Vector3 dirToKeeper = (fm.keeper.transform.position - ball.position).normalized;
        Vector3 pitchPoint = ballPosFlat + (dirToKeeper * (distToKeeper * 1));

        if (distToKeeper <= 21f) pitchPoint -= dirToKeeper * .80f;

        float maxArcHeight = 2.4f;
        float gravity = Mathf.Abs(Physics.gravity.y);

        float verticalVelocity = Mathf.Sqrt(2 * gravity * maxArcHeight);
        float flightTime = 2.5f * (verticalVelocity / gravity);

        float distanceToPitch = Vector3.Distance(ballPosFlat, pitchPoint);
        float horizontalSpeed = distanceToPitch / flightTime;

        Vector3 velocity = dirToKeeper * horizontalSpeed;
        velocity.y = verticalVelocity;
        Debug.DrawLine(ball.position, pitchPoint, Color.red, 15f);
        ballRb.WakeUp();
        ballRb.isKinematic = false;
        ballRb.linearVelocity = velocity;

        fm.keeper.KeeperRecieve(Vector3.zero, ball);

        yield return new WaitForSeconds(0.5f);

        Gameplay.instance.deliveryDead = true;

        StopAllCoroutines();
    }

    #region HelperMethods

    private bool IsBallComingAtFielder(float maxLateralDistance = 0.6f)
    {
        if (ball == null || ballRb == null)
            return false;

        Vector3 ballPos = ball.position;
        Vector3 fielderPos = transform.position;

        Vector3 v = ballRb.linearVelocity;
        v.y = 0f;

        if (v.sqrMagnitude < 0.01f)
            return false;

        Vector3 toFielder = fielderPos - ballPos;
        toFielder.y = 0f;

        float t = Vector3.Dot(toFielder, v) / v.sqrMagnitude;

        if (t <= 0f)
            return false;

        Vector3 closestPoint = ballPos + v * t;

        float missDistance = Vector3.Distance(
            new Vector3(closestPoint.x, 0, closestPoint.z),
            new Vector3(fielderPos.x, 0, fielderPos.z)
        );

        return missDistance <= maxLateralDistance;
    }

    public Vector3 PredictBallPosition(Vector3 startPos, Vector3 velocity, float drag)
    {
        Vector3 currentPos = startPos;
        Vector3 currentVel = velocity;

        float timeStep = Time.fixedDeltaTime;
        Vector3 previousPos = currentPos;

        for (int i = 0; i < 1000; i++)
        {
            previousPos = currentPos;

            currentVel += Physics.gravity * timeStep;
            currentVel *= 1f / (1f + drag * timeStep);
            currentPos += currentVel * timeStep;

            if (currentPos.y <= groundY)
            {
                float t = (previousPos.y - groundY) / (previousPos.y - currentPos.y);
                Vector3 hitPoint = Vector3.Lerp(previousPos, currentPos, t);
                hitPoint.y = groundY;
                return hitPoint;
            }
        }

        return currentPos;
    }

    bool ShouldChase(Transform ball, Vector3 fielderPosition)
    {
        if (ball == null)
            return false;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null)
            return false;

        Vector2 ballVel2D = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z);

        if (ballVel2D.sqrMagnitude < 0.1f)
            return false;

        Vector2 ballDir = ballVel2D.normalized;

        Vector2 ballToFielder = new Vector2(fielderPosition.x - ball.position.x,
                                            fielderPosition.z - ball.position.z);

        float dot = Vector2.Dot(ballDir, ballToFielder);

        return dot < 0;
    }

    #endregion

    public void StopField()
    {
        animator.Play("idle");
        if (isKeeper) KeeperReset();
        else
            Reset();
    }

    public void KeeperReset()
    {
        StopAllCoroutines();
        throwable = false;
        chaseMode = false;
        animator.Play("idle");
        ball = null;
        startedRun = false;
        transform.position = actualPos;
        transform.rotation = Quaternion.Euler(actualRot);
        useAssignedTarget = false;
        this.enabled = false;
    }

    public void Reset()
    {
        throwable = false;
        chaseMode = false;
        animator.Play("idle");
        StopAllCoroutines();
        ball = null;
        startedRun = false;
        transform.position = actualPos;
        transform.rotation = Quaternion.Euler(actualRot);
        useAssignedTarget = false;
        this.enabled = false;
    }
}