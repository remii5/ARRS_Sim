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
    public bool autoUpdate = true;
    public GameObject cameraPrefab;

    private bool needsUpdate = false;
    private List<Transform> cameraList = new List<Transform>();

    [Header("Depth Coverage Settings")]
    public ComputeShader coverageShader;
    public Renderer outputPlane;
    public Vector2Int textureSize = new Vector2Int(512, 512);

    private List<DepthCameraRenderer> depthCameras = new List<DepthCameraRenderer>();
    private RenderTexture resultTexture;

    void Start()
    {
        // Only auto-setup in play mode
        if (Application.isPlaying)
        {
            SetupResultTexture();
        }
    }

    void OnEnable()
    {
        // Rebuild camera references when enabled (don't create new ones)
        RebuildCameraReferences();
    }

    void RebuildCameraReferences()
    {
        // Clear and rebuild lists from existing children
        cameraList.Clear();
        depthCameras.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("KinectCam_"))
            {
                cameraList.Add(child);
                DepthCameraRenderer depthCam = child.GetComponent<DepthCameraRenderer>();
                if (depthCam != null)
                    depthCameras.Add(depthCam);
            }
        }
    }

    void SetupResultTexture()
    {
        if (resultTexture != null)
            resultTexture.Release();

        resultTexture = new RenderTexture(textureSize.x, textureSize.y, 0, RenderTextureFormat.ARGB32);
        resultTexture.enableRandomWrite = true;
        resultTexture.Create();

        if (outputPlane)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                outputPlane.sharedMaterial.mainTexture = resultTexture;
            else
#endif
            outputPlane.material.mainTexture = resultTexture;
        }
    }

    void Update()
    {
        // Only run compute shader in play mode
        if (!Application.isPlaying)
            return;

        if (coverageShader == null || depthCameras.Count == 0)
            return;

        int kernel = coverageShader.FindKernel("CSMain");
        coverageShader.SetTexture(kernel, "Result", resultTexture);

        int camCount = Mathf.Min(depthCameras.Count, 8);

        // Set all 8 texture slots (use first camera's texture as dummy for unused slots)
        RenderTexture dummyTexture = depthCameras[0].depthTexture;

        for (int i = 0; i < 8; i++)
        {
            RenderTexture texToSet = dummyTexture;

            if (i < camCount && depthCameras[i] != null && depthCameras[i].depthTexture != null)
                texToSet = depthCameras[i].depthTexture;

            coverageShader.SetTexture(kernel, $"DepthTextures{i}", texToSet);
        }

        coverageShader.SetInt("numCams", camCount);
        coverageShader.Dispatch(kernel, textureSize.x / 8, textureSize.y / 8, 1);
    }

    private void OnValidate()
    {
        if (autoUpdate && !needsUpdate)
        {
            needsUpdate = true;
#if UNITY_EDITOR
            // Remove any existing delayed calls before adding a new one
            UnityEditor.EditorApplication.delayCall -= DelayedUpdate;
            UnityEditor.EditorApplication.delayCall += DelayedUpdate;
#endif
        }
    }

#if UNITY_EDITOR
    private void DelayedUpdate()
    {
        // Remove the delegate to prevent multiple calls
        UnityEditor.EditorApplication.delayCall -= DelayedUpdate;
        
        if (needsUpdate && this != null)
        {
            needsUpdate = false;
            UpdateCameras();
        }
    }
#endif

    private void OnDisable()
    {
        CleanupResources();
    }

    private void OnDestroy()
    {
        CleanupResources();
    }

    private void CleanupResources()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= DelayedUpdate;
#endif

        if (resultTexture != null)
        {
            resultTexture.Release();
            resultTexture = null;
        }
    }

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

        // Remove excess cameras first
        while (cameraList.Count > numCameras)
        {
            int last = cameraList.Count - 1;
            if (cameraList[last] != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(cameraList[last].gameObject);
                else
#endif
                Destroy(cameraList[last].gameObject);
            }
            cameraList.RemoveAt(last);
            if (last < depthCameras.Count)
                depthCameras.RemoveAt(last);
        }

        // Add missing cameras
        while (cameraList.Count < numCameras)
        {
            GameObject newCam = null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                newCam = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(cameraPrefab, transform);
            else
#endif
            newCam = Instantiate(cameraPrefab, transform);

            newCam.name = $"KinectCam_{cameraList.Count}";

            // Ensure it has a DepthCameraRenderer
            DepthCameraRenderer depthCam = newCam.GetComponent<DepthCameraRenderer>();
            if (depthCam == null)
                depthCam = newCam.AddComponent<DepthCameraRenderer>();

            // Assign RenderTexture and configure
            depthCam.InitializeDepthTexture(textureSize.x, textureSize.y);

            cameraList.Add(newCam.transform);
            depthCameras.Add(depthCam);
        }

        // Position all cameras
        float perimeter = 2f * (arenaWidth + arenaDepth);
        float segmentLength = perimeter / numCameras;
        float distanceTraveled = 0f;

        Vector3[] corners = new Vector3[]
        {
            new Vector3(-arenaWidth/2f, cameraHeight, -arenaDepth/2f),
            new Vector3(arenaWidth/2f, cameraHeight, -arenaDepth/2f),
            new Vector3(arenaWidth/2f, cameraHeight, arenaDepth/2f),
            new Vector3(-arenaWidth/2f, cameraHeight, arenaDepth/2f)
        };

        for (int i = 0; i < numCameras && i < cameraList.Count; i++)
        {
            if (cameraList[i] == null) continue;

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