using UnityEngine;

public enum FurnitureCategory {
    Undefined,
    Table,
    Chair,
    Bed,
    Cabinet,
    Sofa
}

// Define the enum for the dropdown menu
public enum ForwardDirection {
    Forward,
    Back,
    Left,
    Right
}

public class FurnitureData : MonoBehaviour {
    [Header("Basic Information")]
    public FurnitureCategory category = FurnitureCategory.Undefined;

    [Header("Forward Face")]
    [Tooltip("Select which local axis represents the front of the object.")]
    public ForwardDirection forwardFace = ForwardDirection.Forward;

    public Vector3 ForwardVector {
        get {
            switch (forwardFace) {
                case ForwardDirection.Back:
                    return Vector3.back;
                case ForwardDirection.Left:
                    return Vector3.left;
                case ForwardDirection.Right:
                    return Vector3.right;
                case ForwardDirection.Forward:
                default:
                    return Vector3.forward;
            }
        }
    }

    [Header("Clearance Padding (Extra space around object)")]
    public float clearanceFront = 0.05f;
    public float clearanceBack = 0.05f;
    public float clearanceLeft = 0.05f;
    public float clearanceRight = 0.05f;

    [Header("Attachment Slots (e.g., for chairs)")]
    public Vector3[] slots;

    // Dynamically calculating size for visualization
    private Vector3 CalculateSize(BoxCollider bc, Vector3 actualForward, Vector3 actualRight) {
        float newSizeX = bc.size.x
          + Mathf.Abs(actualForward.x) * (clearanceFront + clearanceBack)
          + Mathf.Abs(actualRight.x) * (clearanceLeft + clearanceRight);

        float newSizeZ = bc.size.z
          + Mathf.Abs(actualForward.z) * (clearanceFront + clearanceBack)
          + Mathf.Abs(actualRight.z) * (clearanceLeft + clearanceRight);

        float newSizeY = bc.size.y;

        return new Vector3(newSizeX, newSizeY, newSizeZ);
    }

    private void DrawClearanceArea(Vector3 finalCenter, Vector3 finalSize) {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawCube(finalCenter, finalSize);
        Gizmos.DrawWireCube(finalCenter, finalSize);
    }

    private void DrawForwardArrow(Vector3 actualForward) {
        Gizmos.color = Color.blue;
        // Start the arrow slightly above the ground
        Vector3 startPos = Vector3.zero + Vector3.up * 0.1f;
        Vector3 forwardPos = actualForward * 1.5f;

        Gizmos.DrawLine(startPos, forwardPos);

        // Calculate arrow head rotation based on the selected forward vector
        Quaternion arrowRotation = Quaternion.LookRotation(actualForward);
        Vector3 rightWing = arrowRotation * new Vector3(0.2f, 0, -0.2f);
        Vector3 leftWing = arrowRotation * new Vector3(-0.2f, 0, -0.2f);

        Gizmos.DrawLine(forwardPos, forwardPos + rightWing);
        Gizmos.DrawLine(forwardPos, forwardPos + leftWing);
    }

    private void DrawSlots() {
        // TODO
        if (slots != null && slots.Length > 0) {
            Gizmos.color = Color.green;
            foreach (Vector3 slot in slots) {
                Gizmos.DrawSphere(slot, 0.15f);
            }
        }
    }

    private void OnDrawGizmosSelected() {
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc == null)
            return;

        // actualForward is selected by user (Forward vector is default)
        Vector3 actualForward = ForwardVector;
        // Vector3.Cross returns a perpendicular vector (Right) based on Up and Forward
        Vector3 actualRight = Vector3.Cross(Vector3.up, actualForward);

        Vector3 finalSize = CalculateSize(bc, actualForward, actualRight);

        // Calculate offset dynamically by multiplying our direction vectors with the padding differences
        Vector3 offset = actualForward * ((clearanceFront - clearanceBack) / 2f)
           + actualRight * ((clearanceRight - clearanceLeft) / 2f);

        Vector3 finalCenter = bc.center + offset;

        DrawClearanceArea(finalCenter, finalSize);
        DrawForwardArrow(actualForward);
        DrawSlots();
    }
}