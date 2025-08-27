using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField] private Transform target;   // Drag the Player here in the Inspector
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -7f);
    [SerializeField] private float smoothSpeed = 10f; // Higher = snappier follow, lower = smoother

    void LateUpdate()
    {
        if (target == null) return;

        // Desired position is player's position plus the offset
        Vector3 desiredPosition = target.position + offset;

        // Smoothly interpolate from current to desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Apply position
        transform.position = smoothedPosition;

        // Always look at the player (optional, remove if you want a fixed rotation)
        transform.LookAt(target);
    }
}
