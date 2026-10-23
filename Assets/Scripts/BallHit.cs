using UnityEngine;
using System.Collections;

public class BallHit : MonoBehaviour
{
    Rigidbody rb;
    public bool secondTouch, groundShot, keeperReceive, fielderReached, boundary, stopTriggered;
    public GameObject fieldedPlayer, shootMarker, stopper;
    public Vector3 pitchPoint, ballCatchPoint, shotPoint, shotForce;
    [SerializeField] AudioSource soundFx;
    [SerializeField] AudioClip wicketFx, shotFx;
    [SerializeField] public Fielder keeper;
    [SerializeField] float ballCollectionForward;

    [SerializeField] bool debugBowlingSpeed;
    bool speedDebugActive;
    float speedDebugTimer;
    float speedDebugElapsed;

    public string lastHit;

    public bool keeperExit;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void StartSpeedDebug()
    {
        if (!debugBowlingSpeed) return;

        speedDebugActive = true;
        speedDebugTimer = 0f;
        speedDebugElapsed = 0f;

        Debug.Log($"BOWLING DEBUG START | Position: {rb.position} | Velocity: {rb.linearVelocity} | Speed: {rb.linearVelocity.magnitude * 3.6f:F1} km/h | Damping: {rb.linearDamping}");
    }

    void FixedUpdate()
    {
        if (!speedDebugActive || rb == null) return;

        speedDebugElapsed += Time.fixedDeltaTime;
        speedDebugTimer += Time.fixedDeltaTime;

        if (speedDebugTimer < 0.1f) return;

        speedDebugTimer = 0f;

        Vector3 velocity = rb.linearVelocity;

        Debug.Log($"BOWLING SPEED | Time: {speedDebugElapsed:F2}s | Total: {velocity.magnitude * 3.6f:F1} km/h | Horizontal: {new Vector3(velocity.x, 0f, velocity.z).magnitude * 3.6f:F1} km/h | Vertical: {velocity.y:F2} m/s | Position: {rb.position} | Kinematic: {rb.isKinematic} | Damping: {rb.linearDamping}");

        if (speedDebugElapsed >= 3f || rb.isKinematic)
            speedDebugActive = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        lastHit = collision.gameObject.tag;

        switch (collision.gameObject.tag)
        {
            case "Wicket":
                if (fielderReached || Gameplay.instance.deliveryDead) return;
                soundFx.PlayOneShot(wicketFx);
                //Debug.Log("Stump crash");
                Gameplay.instance.Out();
                Gameplay.instance.deliveryDead = true;
                break;

            case "pitch":
                //Debug.Log($"PITCH COLLISION | Velocity after collision: {rb.linearVelocity} | Speed: {rb.linearVelocity.magnitude * 3.6f:F1} km/h | Horizontal: {new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude * 3.6f:F1} km/h");
                if (secondTouch)
                {
                    if (groundShot) return;
                    groundShot = true;
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * 0.04f, rb.linearVelocity.z);
                }
                else
                {
                    pitchPoint = collision.contacts[0].point;
                    shotForce = rb.linearVelocity;
                    StartCoroutine(SimulateBallTrajectory(transform.position, rb.linearVelocity));
                }
                break;

            case "Bat":
                if (secondTouch)
                {
                    break;
                }
                shotPoint = collision.contacts[0].point;
                shotForce = rb.linearVelocity;
                FieldManager.hitBallPos = transform.position;
                FieldManager.hitVelocity = rb.linearVelocity;
                Debug.Log($"BAT HIT SNAPSHOT | Position: {FieldManager.hitBallPos} | Velocity: {FieldManager.hitVelocity} | Speed: {FieldManager.hitVelocity.magnitude:F2}");
                //Debug.Log("spot " + collision.gameObject.name);
                Gameplay.instance.broadcastCamComp.readyToDeliver = false;
                secondTouch = true;
                soundFx.PlayOneShot(shotFx);
                shootMarker.transform.position = PredictFallPosition(transform.position, FieldManager.hitVelocity, -4.44f);
                FieldManager.StartCheckField.Invoke(shotPoint);
                break;

            case "Ground":
                if (secondTouch && !groundShot)
                {
                    groundShot = true;
                }
                break;

            case "boundary":
                FieldManager.StopOther?.Invoke(null);
                Debug.Log("Stopped by " + collision.gameObject.name);
                stopTriggered = true;
                boundary = true;
                Gameplay.instance.deliveryDead = true;
                break;

            case "gallery":
                Gameplay.instance.deliveryDead = true;
                break;

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("keeper") || (other.gameObject.CompareTag("rayTest") && other.transform.parent.CompareTag("keeper")))
        {
            keeperReceive = true;
            if (secondTouch)
            {
                fieldedPlayer = other.transform.parent.gameObject;
                fielderReached = true;
                return;
            }
        }

        if (other.gameObject.tag is "fielder" or "DeepFielder")
        {
            if (secondTouch)
            {
                fieldedPlayer = other.transform.parent.gameObject;
                fielderReached = true;
            }
        }

        if(other.gameObject.tag is "boundary")
        {
            FieldManager.StopOther?.Invoke(null);
            Debug.Log("Stopped by " + other.gameObject.name);
            stopTriggered = true;
            boundary = true;
            Gameplay.instance.deliveryDead = true;
            return;
        }

        if (other.gameObject.CompareTag("stop"))
        {
            if (stopTriggered) return;

            //Debug.Log("stop");
            stopTriggered = true;
            stopper = other.gameObject;

            Transform parent = stopper.transform;
            bool keeperStop = false;

            while (parent != null)
            {
                if (parent.CompareTag("keeper"))
                {
                    keeperStop = true;
                    break;
                }

                parent = parent.parent;
            }

            if (keeperStop)
            {
                Transform hand = stopper.transform.parent;

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;

                transform.position = other.transform.position;
                transform.SetParent(hand, true);

                //Debug.Log($"Keeper caught ball at hand trigger: {hand.name}");
            }

            FieldManager.StopOther?.Invoke(stopper.GetComponent<BallStopper>().fieldScript);
        }


        if (other.gameObject.CompareTag("camTrigger"))
        {
            Vector3 contactPoint = transform.position;
            CheckLegalDelivery(contactPoint);
            cover = true;
        }
    }
    public bool cover = false;

    Vector3 PredictFallPosition(Vector3 startPos, Vector3 velocity, float groundY, float timeStep = 0.02f)
    {
        Vector3 pos = startPos;
        Vector3 vel = velocity;
        float gravity = Physics.gravity.y;

        while (pos.y > groundY)
        {
            vel.y += gravity * timeStep;
            pos += vel * timeStep;

            if (pos.y <= groundY)
            {
                float overshoot = groundY - pos.y;
                float totalDisplacement = vel.y * timeStep;
                float factor = overshoot / totalDisplacement;
                Vector3 lastPos = pos - vel * timeStep;
                return Vector3.Lerp(lastPos, pos, factor);
            }
        }

        return pos; // fallback
    }

    [SerializeField] LayerMask keeperLayer;
    

    IEnumerator SimulateBallTrajectory(Vector3 startPosition, Vector3 initialVelocity)
    {
        float timestep = Time.fixedDeltaTime;
        float maxTime = 2f;
        float ballRadius = GetComponent<SphereCollider>().radius * transform.lossyScale.x;
        //Debug.Log(ballRadius + " ra");
        float drag = rb.linearDamping;

        Vector3 currentPosition = startPosition;
        Vector3 velocity = initialVelocity;

        int totalSteps = Mathf.CeilToInt(maxTime / timestep);

        for (int s = 0; s < totalSteps; s++)
        {
            velocity += Physics.gravity * timestep;
            velocity /= (1f + drag * timestep);

            Vector3 moveDelta = velocity * timestep;
            float moveDistance = moveDelta.magnitude;

            if (moveDistance > 0.0001f)
            {
                if (Physics.SphereCast(currentPosition, ballRadius, moveDelta.normalized, out RaycastHit hit, moveDistance, keeperLayer, QueryTriggerInteraction.Collide))
                {
                    //Debug.Log($"Prediction hit: {hit.collider.name} | Tag: {hit.collider.tag} | Layer: {hit.collider.gameObject.layer}");

                    BallStopper stopper = hit.collider.GetComponent<BallStopper>();

                    if (hit.collider.CompareTag("keeper") ||
                        (hit.collider.CompareTag("rayTest") && hit.collider.transform.parent != null && hit.collider.transform.parent.CompareTag("keeper")) ||
                        (hit.collider.CompareTag("stop") && stopper != null && stopper.fieldScript != null && stopper.fieldScript.isKeeper))
                    {
                        Vector3 fixedCatchPoint = hit.point;
                        fixedCatchPoint.z = ballCollectionForward;
                        ballCatchPoint = fixedCatchPoint;

                        shootMarker.transform.position = ballCatchPoint;

                        //Debug.Log($"KEEPER TARGET REQUEST | Point: {ballCatchPoint} | Keeper X: {keeper.transform.position.x:F2} | Delta X: {ballCatchPoint.x - keeper.transform.position.x:F2}");
                        FieldManager.KeeperCollect?.Invoke(ballCatchPoint, transform, false);
                        yield break;
                    }
                }

            }

            // Simulate pitch/ground collision, including bounce or rolling.

            if (Physics.SphereCast(currentPosition, ballRadius, Vector3.down, out RaycastHit groundHit, ballRadius + 0.5f, Physics.AllLayers, QueryTriggerInteraction.Ignore) && groundHit.normal.y > 0.5f)
            {
                float groundDistance = currentPosition.y - (groundHit.point.y + ballRadius);

                if (groundDistance <= 0.05f && Vector3.Dot(velocity, groundHit.normal) < 0f)
                {
                    currentPosition.y = groundHit.point.y + ballRadius;

                    float restitution = groundHit.collider.sharedMaterial != null ? groundHit.collider.sharedMaterial.bounciness : 0f;
                    float normalVelocity = Vector3.Dot(velocity, groundHit.normal);

                    velocity -= (1f + restitution) * normalVelocity * groundHit.normal;

                    if (Mathf.Abs(velocity.y) < 0.5f)
                        velocity.y = 0f;
                }
            }



            currentPosition += velocity * timestep;

            Debug.DrawRay(currentPosition, Vector3.up * 0.2f, Color.yellow, 2f);
        }

        yield return null;
    }


    void CheckLegalDelivery(Vector3 enterPos)
    {
        if (enterPos.z is <= -4.71f or >= 1.8f || enterPos.y > 2.96f)
        {
            //Debug.Log("wideball");
            //Gameplay.instance.legalDelivery = false;
        }
        else
        {
            //Debug.Log("goodball");
            //Gameplay.instance.legalDelivery = true;
        }
    }

    public void Reset()
    {
        lastHit = "";
        cover = false;
        GetComponent<Rigidbody>().isKinematic = true;
        secondTouch = false;
        groundShot = false;
        keeperReceive = false;
        fielderReached = false;
        boundary = false;
        stopTriggered = false;
        keeperExit = false;
    }
}
