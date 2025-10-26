using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class CameraRingController : MonoBehaviour
{

    public int numCameras = 4;
    public float arenaWidth = 5f;
    public float arenaDepth = 3f;
    public float cameraHeight = 2.94f;
    public bool faceCenter = true;
    public bool autoUpdate = true;
    public GameObject cameraPrefab;


    private List<Transform> cameraList = new List<Transform>();
    private bool needsUpdate = false;

    private void OnValidate()
    {
        if (autoUpdate)
        {
            // Schedule update for next editor frame instead of immediate execution
            needsUpdate = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += DelayedUpdate;
#endif
        }
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
    /// Regenerates or repositions all cameras to fit the rectangular ring.
    /// </summary>
    public void UpdateCameras()
    {
        if (cameraPrefab == null)
        {
            Debug.LogWarning("Camera prefab not assigned to CameraRingController.");
            return;
        }

        // Ensure correct number of cameras
        while (cameraList.Count < numCameras)
        {
            GameObject newCam = Instantiate(cameraPrefab, transform);
            newCam.name = $"KinectCam_{cameraList.Count}";
            cameraList.Add(newCam.transform);
        }

        while (cameraList.Count > numCameras)
        {
            Transform camToRemove = cameraList[cameraList.Count - 1];
            if (camToRemove != null)
                DestroyImmediate(camToRemove.gameObject);
            cameraList.RemoveAt(cameraList.Count - 1);
        }

        // Compute total perimeter and segment spacing
        float perimeter = 2f * (arenaWidth + arenaDepth);
        float segmentLength = perimeter / numCameras;
        float distanceTraveled = 0f;

        // Define the 4 rectangle sides (clockwise)
        Vector3[] corners = new Vector3[]
        {
            new Vector3(-arenaWidth/2f, cameraHeight, -arenaDepth/2f), // bottom-left
            new Vector3(arenaWidth/2f, cameraHeight, -arenaDepth/2f),  // bottom-right
            new Vector3(arenaWidth/2f, cameraHeight, arenaDepth/2f),   // top-right
            new Vector3(-arenaWidth/2f, cameraHeight, arenaDepth/2f)   // top-left
        };

        // Place cameras evenly along edges
        for (int i = 0; i < numCameras; i++)
        {
            float t = distanceTraveled / perimeter;
            Vector3 pos = GetPointOnRectangle(corners, t);
            cameraList[i].localPosition = pos;

            if (faceCenter)
                cameraList[i].LookAt(transform.position);

            distanceTraveled += segmentLength;
        }
    }

    /// <summary>
    /// Returns a point along the perimeter of a rectangle defined by 4 corners (clockwise).
    /// t ∈ [0,1) represents normalized distance around perimeter.
    /// </summary>
    private Vector3 GetPointOnRectangle(Vector3[] corners, float t)
    {
        float totalLength = 0f;
        float[] edgeLengths = new float[4];

        // Compute edge lengths
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