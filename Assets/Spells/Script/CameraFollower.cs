using UnityEngine;

public class UIOnHandFollower : MonoBehaviour
{
    [Header("Hand Reference")]
    public Transform rightHandController; // Drag your right hand controller here

    [Header("Position Settings")]
    public Vector3 handOffset = new Vector3(0.1f, 0.1f, 0f); // Offset from hand
    public float followSmoothness = 10f;

    [Header("Rotation Settings")]
    public bool facePlayer = true;
    public bool keepUpright = true;

    [Header("Screen Edge Protection")]
    public bool stayInView = true;
    public float viewMargin = 0.1f;

    private Camera playerCamera;
    private Vector3 lastValidPosition;
    private Quaternion lastValidRotation;

    void Start()
    {
        playerCamera = Camera.main;

        if (rightHandController == null)
        {
            Debug.LogError("Right Hand Controller not assigned! Looking for XR controller...");

            // Try to find it automatically
            GameObject rightHand = GameObject.Find("RightHand Controller") ??
                                  GameObject.Find("Right Hand") ??
                                  GameObject.Find("RightController");

            if (rightHand != null)
            {
                rightHandController = rightHand.transform;
                Debug.Log($"Found right hand: {rightHand.name}");
            }
        }

        if (rightHandController == null)
        {
            Debug.LogError("Could not find right hand controller! Disabling script.");
            enabled = false;
            return;
        }

        // Initial position
        UpdatePositionImmediate();
    }

    void LateUpdate()
    {
        if (rightHandController == null || !IsValidTransform(rightHandController)) return;

        // Calculate target position on hand
        Vector3 targetPosition = CalculateHandPosition();
        Quaternion targetRotation = CalculateHandRotation();

        // Validate
        if (!IsValidVector3(targetPosition) || !IsValidQuaternion(targetRotation))
        {
            Debug.LogWarning("Invalid position/rotation calculated, using last valid");
            targetPosition = lastValidPosition;
            targetRotation = lastValidRotation;
        }

        // Apply with smoothing
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSmoothness * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, followSmoothness * Time.deltaTime);

        // Keep in view if enabled
        if (stayInView && playerCamera != null)
        {
            KeepInView();
        }

        // Store valid values
        lastValidPosition = transform.position;
        lastValidRotation = transform.rotation;
    }

    Vector3 CalculateHandPosition()
    {
        // Position relative to hand
        return rightHandController.position +
               rightHandController.right * handOffset.x +
               rightHandController.up * handOffset.y +
               rightHandController.forward * handOffset.z;
    }

    Quaternion CalculateHandRotation()
    {
        if (facePlayer && playerCamera != null)
        {
            // Face the player/camera
            Vector3 directionToPlayer = playerCamera.transform.position - transform.position;

            if (keepUpright)
            {
                directionToPlayer.y = 0; // Keep UI upright
            }

            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                return Quaternion.LookRotation(-directionToPlayer);
            }
        }

        // Default: match hand rotation but keep readable
        return Quaternion.LookRotation(-rightHandController.forward, Vector3.up);
    }

    void KeepInView()
    {
        // Convert UI position to screen space
        Vector3 screenPos = playerCamera.WorldToViewportPoint(transform.position);

        bool needsAdjustment = false;
        Vector3 adjustment = Vector3.zero;

        // Check if UI is going off-screen
        if (screenPos.x < viewMargin) // Too far left
        {
            adjustment += playerCamera.transform.right * 0.05f;
            needsAdjustment = true;
        }
        else if (screenPos.x > 1 - viewMargin) // Too far right
        {
            adjustment -= playerCamera.transform.right * 0.05f;
            needsAdjustment = true;
        }

        if (screenPos.y < viewMargin) // Too far down
        {
            adjustment += playerCamera.transform.up * 0.05f;
            needsAdjustment = true;
        }
        else if (screenPos.y > 1 - viewMargin) // Too far up
        {
            adjustment -= playerCamera.transform.up * 0.05f;
            needsAdjustment = true;
        }

        if (screenPos.z < 0) // Behind camera
        {
            adjustment += playerCamera.transform.forward * 0.1f;
            needsAdjustment = true;
        }

        if (needsAdjustment)
        {
            transform.position += adjustment;
        }
    }

    void UpdatePositionImmediate()
    {
        Vector3 pos = CalculateHandPosition();
        Quaternion rot = CalculateHandRotation();

        if (IsValidVector3(pos) && IsValidQuaternion(rot))
        {
            transform.position = pos;
            transform.rotation = rot;
            lastValidPosition = pos;
            lastValidRotation = rot;
        }
    }

    // ===== UTILITY METHODS =====
    bool IsValidTransform(Transform t)
    {
        return t != null &&
               IsValidVector3(t.position) &&
               IsValidQuaternion(t.rotation);
    }

    bool IsValidVector3(Vector3 v)
    {
        return !float.IsNaN(v.x) && !float.IsInfinity(v.x) &&
               !float.IsNaN(v.y) && !float.IsInfinity(v.y) &&
               !float.IsNaN(v.z) && !float.IsInfinity(v.z);
    }

    bool IsValidQuaternion(Quaternion q)
    {
        return !float.IsNaN(q.x) && !float.IsInfinity(q.x) &&
               !float.IsNaN(q.y) && !float.IsInfinity(q.y) &&
               !float.IsNaN(q.z) && !float.IsInfinity(q.z) &&
               !float.IsNaN(q.w) && !float.IsInfinity(q.w);
    }

    // Quick position reset
    public void SnapToHand()
    {
        UpdatePositionImmediate();
    }

    // Draw debug gizmo in editor
    void OnDrawGizmosSelected()
    {
        if (rightHandController != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(rightHandController.position, CalculateHandPosition());
            Gizmos.DrawWireSphere(CalculateHandPosition(), 0.05f);
        }
    }
}