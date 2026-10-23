using UnityEngine;
using RootMotion.FinalIK;
using System.Collections;

public class SmoothInteractionPickup : MonoBehaviour
{
    [Header("Main Components")]
    public InteractionSystem interactionSystem;
    public FullBodyBipedIK fbbik; // Assign this in Inspector
    public InteractionObject pickupObject;
    [Min(0.05f)] public float keeperReachDuration = 0.38f;

    [Header("Natural Movement Settings")]
    [Tooltip("How fast the head turns to look at the target")]
    public float lookAtSmoothSpeed = 5f;
    [Tooltip("How much the character looks at the object (0-1)")]
    [Range(0f, 1f)] public float lookAtIntensity = 0.8f;
    public float holdDuration = 2.0f;

    [Header("Effectors")]
    public FullBodyBipedEffector rightHandEffector = FullBodyBipedEffector.RightHand;
    public FullBodyBipedEffector leftHandEffector = FullBodyBipedEffector.LeftHand;

    // Internal State
    private bool isHolding = false;
    public bool IsHolding => isHolding;   // NEW — read-only accessor
    public bool isTransitioning = false;
    private float currentLookWeight = 0f;
    private Transform currentLookTarget;

    public bool IsStrafing => strafeActive;

    [SerializeField] Transform throwArm;

    void Start()
    {
        if (interactionSystem == null) interactionSystem = GetComponent<InteractionSystem>();
        if (fbbik == null) fbbik = GetComponent<FullBodyBipedIK>();

        // Safety check to prevent weird initial snaps
        if (fbbik != null)
        {
            //fbbik.solver..weight = 0f;
        }

        //Debug.Log("Smooth Pickup Ready. Press E to Interact.");
    }

    // Example: Moving the foot AND shifting the body


    [Header("Strafe Motion")]
    public float stepDistance = 0.5f; // Length of one step
    public float stepHeight = 0.2f;   // Height of the arc
    public float stepSpeed = 180f;    // Speed of the animation

    private float stepCycle = 0f;
    public float EstimatedStrafeSpeed => stepDistance * 2f * stepSpeed * Mathf.Deg2Rad / Mathf.PI;

    // Call this every frame in your loop.
    // returns TRUE when the character has finished a full step (useful for your logic)

    private int strafePhase;
    private float strafeProgress;
    private float activeStrafeDirection;
    private bool strafeActive;

    public bool StrafeStep(float direction)
    {
        if (fbbik == null || fbbik.solver == null)
            return false;

        if (Mathf.Abs(direction) < 0.01f && !strafeActive)
            return true;

        direction = Mathf.Sign(direction);

        if (!strafeActive || direction != activeStrafeDirection)
        {
            ResetOffsets();
            strafeActive = true;
            activeStrafeDirection = direction;
        }

        float distance = Mathf.Max(stepDistance * 0.6f, 0.01f);
        float speed = Mathf.Max(EstimatedStrafeSpeed * 2.6f, 0.01f);
        float phaseDuration = distance / speed;

        strafeProgress += Time.deltaTime / phaseDuration;

        if (strafeProgress >= 1f)
        {
            strafeProgress = 0f;
            strafePhase++;

            if (strafePhase >= 3)
            {
                strafePhase = 0;
                strafeActive = false;
                return true;
            }
        }

        float t = Mathf.Clamp01(strafeProgress);
        Vector3 lateral = Vector3.right * direction;

        bool movingLeft = direction > 0f;
        IKEffector leadLeg = movingLeft ? fbbik.solver.leftFootEffector : fbbik.solver.rightFootEffector;
        IKEffector trailLeg = movingLeft ? fbbik.solver.rightFootEffector : fbbik.solver.leftFootEffector;

        leadLeg.positionOffset = Vector3.zero;
        trailLeg.positionOffset = Vector3.zero;
        fbbik.solver.bodyEffector.positionOffset = Vector3.zero;

        if (strafePhase == 0)
        {
            float lift = Mathf.Sin(t * Mathf.PI) * stepHeight * 0.2f;

            leadLeg.positionOffset = lateral * distance * t + Vector3.up * lift;
        }
        else if (strafePhase == 1)
        {
            if (Mathf.Abs(direction) > 0.01f)
                transform.position += lateral * speed * Time.deltaTime;

            leadLeg.positionOffset = lateral * distance * (1f - t);
            trailLeg.positionOffset = -lateral * distance * t;
            fbbik.solver.bodyEffector.positionOffset = lateral * 0.025f * Mathf.Sin(t * Mathf.PI);
        }
        else
        {
            float lift = Mathf.Sin(t * Mathf.PI) * stepHeight * 0.2f;

            trailLeg.positionOffset = -lateral * distance * (1f - t) + Vector3.up * lift;
        }

        return false;
    }

    public void ResetOffsets()
    {
        if (fbbik != null && fbbik.solver != null)
        {
            fbbik.solver.rightFootEffector.positionOffset = Vector3.zero;
            fbbik.solver.leftFootEffector.positionOffset = Vector3.zero;
            fbbik.solver.bodyEffector.positionOffset = Vector3.zero;
        }

        strafePhase = 0;
        strafeProgress = 0f;
        activeStrafeDirection = 0f;
        strafeActive = false;
    }

    public void CancelStrafe()
    {
        ResetOffsets();
    }


    public void StartKeeperPickup(bool singleHand, Fielder fielder, Vector3 catchPoint)
    {
        StartCoroutine(KeeperPickupSequence(singleHand, fielder, catchPoint));
    }

    IEnumerator KeeperPickupSequence(bool singleHand, Fielder fielder, Vector3 catchPoint)
    {
        isTransitioning = true;

        Transform rightHand = fbbik.references.rightHand;
        Transform leftHand = fbbik.references.leftHand;

        IKEffector rightEffector = fbbik.solver.rightHandEffector;
        IKEffector leftEffector = fbbik.solver.leftHandEffector;

        Vector3 rightStart = rightHand.position;
        Vector3 leftStart = leftHand.position;

        catchPoint.z = 25f;

        Vector3 rightTarget = catchPoint + transform.right * 0.1f;
        Vector3 leftTarget = catchPoint - transform.right * 0.1f;

        rightEffector.position = rightStart;
        rightEffector.positionWeight = 1f;

        if (!singleHand)
        {
            leftEffector.position = leftStart;
            leftEffector.positionWeight = 1f;
        }

        float elapsed = 0f;
        float reachDuration = keeperReachDuration;

        while (elapsed < reachDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / reachDuration);
            t = t * t * (3f - 2f * t);

            rightEffector.position = Vector3.Lerp(rightStart, rightTarget, t);

            if (!singleHand)
                leftEffector.position = Vector3.Lerp(leftStart, leftTarget, t);

            yield return null;
        }

        rightEffector.position = rightTarget;

        if (!singleHand)
            leftEffector.position = leftTarget;


        Rigidbody ballRb = pickupObject.GetComponent<Rigidbody>();

        if (ballRb != null)
        {
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.isKinematic = true;
        }

        if (pickupObject.transform.parent != rightHand &&
            pickupObject.transform.parent != leftHand)
        {
            pickupObject.transform.position = rightHand.position;
            pickupObject.transform.SetParent(rightHand, true);
        }

        isHolding = true;

        float returnDuration = 0.3f;
        float returnElapsed = 0f;

        while (returnElapsed < returnDuration)
        {
            returnElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(returnElapsed / returnDuration);
            t = t * t * (3f - 2f * t);

            rightEffector.positionWeight = 1f - t;

            if (!singleHand)
                leftEffector.positionWeight = 1f - t;

            yield return null;
        }

        rightEffector.positionWeight = 0f;

        if (!singleHand)
            leftEffector.positionWeight = 0f;

        rightEffector.positionOffset = Vector3.zero;
        leftEffector.positionOffset = Vector3.zero;

        isTransitioning = false;

        fielder.PickupComplete();
    }

    public void StartPickup(bool singleHand, Fielder fl)
    {
        StartCoroutine(PickupSequence(singleHand, fl));
    }

    IEnumerator PickupSequence(bool singleHand, Fielder fielder)
    {
        Debug.Log($"[IK] StartPickup target at {pickupObject.transform.position}  myPos {transform.position}  dist {Vector3.Distance(pickupObject.transform.position, transform.position):F2}");
        isTransitioning = true;

        // 1. ANTICIPATION: Look at the ball first
        currentLookTarget = pickupObject.transform;
        currentLookWeight = lookAtIntensity;
        interactionSystem.lookAt.lookAtTarget = currentLookTarget;

        yield return new WaitForSeconds(0.1f);

        if (!singleHand)
        {
            interactionSystem.StartInteraction(leftHandEffector, pickupObject, false);
        }
        interactionSystem.StartInteraction(rightHandEffector, pickupObject, false);
        interactionSystem.lookAt.lookAtTarget = pickupObject.transform;

        Debug.Log("Fielder reaching for ball..."+Time.time);

        while (!interactionSystem.IsPaused(rightHandEffector))
        {
            if (!interactionSystem.IsInInteraction(rightHandEffector))
            {
                isTransitioning = false;
                yield break;
            }
            yield return null;
        }
        Debug.Log("reach Complete." + Time.time);

        isHolding = true;

        if (holdDuration > 0) yield return new WaitForSeconds(holdDuration);

        interactionSystem.ResumeInteraction(rightHandEffector);
        if (!singleHand) interactionSystem.ResumeInteraction(leftHandEffector);

        while (interactionSystem.IsInInteraction(rightHandEffector))
        {
            yield return null;
        }

        interactionSystem.speed = 1.0f;

        isTransitioning = false;
        Debug.Log("Pickup Sequence Complete.");
        fielder.PickupComplete();
    }

    public void StartDrop()
    {
        StartCoroutine(DropSequence());
    }

    IEnumerator DropSequence()
    {
        isTransitioning = true;
        Debug.Log("Dropping...");

        // Step 1: Release Hands
        interactionSystem.StopInteraction(rightHandEffector);
        interactionSystem.StopInteraction(leftHandEffector);

        // Step 2: Lose Focus
        // We stop looking at the object as we let go
        currentLookWeight = 0f;

        // Step 3: Wait for Blend Out
        // The interaction system takes time to blend weights back to 0. 
        // We wait here to ensure the coroutine doesn't finish while the character is still moving.
        //yield return new WaitForSeconds(1.0f);
        yield return null;
        isHolding = false;
        isTransitioning = false;
        currentLookTarget = null; // Clear target to prevent "ghost" looking
    }
}