using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class CameraRingController : MonoBehaviour
{
    [Header("Ring Settings")]
    public int numCameras = 4;
    public float arenaWidth = 5f;
    public float arenaDepth = 3f;
    public float cameraHeight = 2.94f;
    public bool faceCenter = true;

    [Header("Behavior")]
    public bool autoUpdate = true;
    public GameObject cameraPrefab;

    [Header("Auto-Setup")]
    public VoxelGrid voxelGrid;
    public CoverageVisualizer coverageVisualizer;

    private List<Transform> cameraList = new List<Transform>();
    private bool needsUpdate = false;

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && autoUpdate)
        {
            needsUpdate = true;
            UnityEditor.EditorApplication.delayCall += DelayedUpdate;
        }
#endif
    }

#if UNITY_EDITOR
    private void DelayedUpdate()
    {
        if (needsUpdate && this != null)
        {
            needsUpdate = false;
            UpdateCameras();
        }
    }
#endif

    /// <summary>
    /// Creates or repositions Kinect cameras along the rectangular ring.
    /// </summary>
    public void UpdateCameras()
    {
        // Prevent regenerating while in Play mode
        if (Application.isPlaying)
            return;

        if (cameraPrefab == null)
        {
            Debug.LogWarning("Camera prefab not assigned to CameraRingController.");
            return;
        }

        // Remove any old generated cameras first
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("KinectCam_"))
                DestroyImmediate(child.gameObject);
        }

        cameraList.Clear();

        // Compute perimeter and spacing
        float perimeter = 2f * (arenaWidth + arenaDepth);
        float segmentLength = perimeter / numCameras;
        float distanceTraveled = 0f;

        // Define the 4 corners (clockwise)
        Vector3[] corners = new Vector3[]
        {
            new Vector3(-arenaWidth/2f, cameraHeight, -arenaDepth/2f), // bottom-left
            new Vector3(arenaWidth/2f, cameraHeight, -arenaDepth/2f),  // bottom-right
            new Vector3(arenaWidth/2f, cameraHeight, arenaDepth/2f),   // top-right
            new Vector3(-arenaWidth/2f, cameraHeight, arenaDepth/2f)   // top-left
        };

        List<KinectSampler> samplers = new List<KinectSampler>();

        // Place cameras evenly around perimeter
        for (int i = 0; i < numCameras; i++)
        {
            GameObject camObj = Instantiate(cameraPrefab, transform);
            camObj.name = $"KinectCam_{i}";
            cameraList.Add(camObj.transform);

            float t = distanceTraveled / perimeter;
            Vector3 pos = GetPointOnRectangle(corners, t);
            camObj.transform.localPosition = pos;

            if (faceCenter)
                camObj.transform.LookAt(transform.position);

            // Auto-setup KinectSampler component
            KinectSampler sampler = camObj.GetComponent<KinectSampler>();
            if (sampler != null && voxelGrid != null)
            {
                sampler.voxelGrid = voxelGrid;
                sampler.cam = camObj.GetComponent<Camera>();
                samplers.Add(sampler);
            }

            distanceTraveled += segmentLength;
        }

        // Auto-populate CoverageVisualizer
        if (coverageVisualizer != null)
        {
            coverageVisualizer.samplers = samplers;
            coverageVisualizer.voxelGrid = voxelGrid;
            Debug.Log($"Auto-assigned {samplers.Count} samplers to CoverageVisualizer");
        }
    }

    /// <summary>
    /// Returns a point along the perimeter of a rectangle defined by 4 corners (clockwise).
    /// </summary>
    private Vector3 GetPointOnRectangle(Vector3[] corners, float t)
    {
        float totalLength = 0f;
        float[] edgeLengths = new float[4];

        for (int i = 0; i < 4; i++)
        {
            edgeLengths[i] = Vector3.Distance(corners[i], corners[(i + 1) % 4]);
            totalLength += edgeLengths[i];
        }

        float targetDistance = t * totalLength;
        float cumulative = 0f;

        for (int i = 0; i < 4; i++)
        {
            float next = cumulative + edgeLengths[i];
            if (targetDistance <= next)
            {
                float localT = (targetDistance - cumulative) / edgeLengths[i];
                return Vector3.Lerp(corners[i], corners[(i + 1) % 4], localT);
            }
            cumulative = next;
        }

        return corners[0];
    }
}