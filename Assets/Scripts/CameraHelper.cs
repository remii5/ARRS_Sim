using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraHelper : MonoBehaviour
{
    public Color gizmoColor = Color.yellow;
    public float rayLength = 5f;

    void OnDrawGizmos()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) return;

        Gizmos.color = gizmoColor;

        // Draw corner rays of frustum
        Vector3[] corners = new Vector3[]
        {
            new Vector3(0, 0, 0),     // bottom-left
            new Vector3(1, 0, 0),     // bottom-right
            new Vector3(1, 1, 0),     // top-right
            new Vector3(0, 1, 0),     // top-left
            new Vector3(0.5f, 0.5f, 0) // center
        };

        foreach (var corner in corners)
        {
            Ray ray = cam.ViewportPointToRay(corner);
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * rayLength);

            // Check if ray hits something
            if (Physics.Raycast(ray, out RaycastHit hit, rayLength))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(hit.point, 0.05f);
                Gizmos.color = gizmoColor;
            }
        }

        // Draw camera name
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, gameObject.name);
#endif
    }
}