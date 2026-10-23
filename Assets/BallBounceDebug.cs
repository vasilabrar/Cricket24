using UnityEngine;

public class BallBounceDebug : MonoBehaviour
{
    public Rigidbody ball;
    private float prevY;
    private bool loggedPitch, loggedStumps;
    private float vyBefore, vyAfter, yAtPitch;

    void FixedUpdate()
    {
        if (ball == null) return;

        // Detect pitch impact (velocity Y flips from negative to positive)
        if (!loggedPitch && prevY < 0 && ball.linearVelocity.y > 0.5f &&
            ball.position.z > -8f && ball.position.z < 8f)
        {
            loggedPitch = true;
            vyBefore = prevY;
            vyAfter = ball.linearVelocity.y;
            yAtPitch = ball.position.y;
            //Debug.Log($"PITCH IMPACT at z={ball.position.z:F2}, y={yAtPitch:F3}. " +
            //          $"vy_before={vyBefore:F2}, vy_after={vyAfter:F2}, " +
            //          $"COR_actual={-vyAfter / vyBefore:F2}");
        }

        // Detect crossing batsman's stumps
        if (!loggedStumps && ball.position.z > 10.06f)
        {
            loggedStumps = true;
            //Debug.Log($"AT STUMPS: y={ball.position.y:F3}, " +
            //          $"vy={ball.linearVelocity.y:F2}, " +
            //          $"speed={ball.linearVelocity.magnitude * 3.6f:F1} km/h");
        }

        prevY = ball.linearVelocity.y;

        // Reset for next delivery
        if (ball.position.z < -9f)
        {
            loggedPitch = loggedStumps = false;
        }
    }
}