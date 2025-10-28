using UnityEngine;
using System.Collections.Generic;

public class VoxelCoverageController : MonoBehaviour
{
    [Header("Voxel Grid Settings")]
    public Vector3 gridMin = new Vector3(-2.5f, 0f, -1.5f);
    public Vector3 gridMax = new Vector3(2.5f, 3f, 1.5f);
    public Vector3Int gridResolution = new Vector3Int(64, 32, 64);

    [Header("Detection Settings")]
    public string objectTagToTrack = "Player"; // Track objects with this tag
    public bool trackAllObjects = true; // Or track everything

    [Header("Visualization")]
    public Renderer outputPlane;
    public Vector2Int outputResolution = new Vector2Int(512, 512);
    public ComputeShader voxelShader;

    [Header("Coverage Settings")]
    [Range(0f, 1f)]
    public float decayRate = 0.01f;
    public bool enablePersistence = true;

    private RenderTexture voxelGrid;
    private RenderTexture outputTexture;
    private List<Camera> coverageCameras = new List<Camera>();
    private ComputeBuffer objectPositionsBuffer;
    private List<Vector3> trackedObjectPositions = new List<Vector3>();

    void Start()
    {
        InitializeVoxelGrid();
        SetupOutput();
        FindCoverageCameras();

        // Create buffer for object positions
        objectPositionsBuffer = new ComputeBuffer(100, sizeof(float) * 3);
    }

    void InitializeVoxelGrid()
    {
        if (voxelGrid != null)
            voxelGrid.Release();

        voxelGrid = new RenderTexture(gridResolution.x, gridResolution.y, 0, RenderTextureFormat.RFloat);
        voxelGrid.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        voxelGrid.volumeDepth = gridResolution.z;
        voxelGrid.enableRandomWrite = true;
        voxelGrid.Create();

        if (voxelShader != null)
        {
            int clearKernel = voxelShader.FindKernel("ClearVoxels");
            voxelShader.SetTexture(clearKernel, "VoxelGrid", voxelGrid);
            voxelShader.Dispatch(clearKernel,
                Mathf.CeilToInt(gridResolution.x / 8f),
                Mathf.CeilToInt(gridResolution.y / 8f),
                Mathf.CeilToInt(gridResolution.z / 8f));
        }
    }

    void SetupOutput()
    {
        if (outputTexture != null)
            outputTexture.Release();

        outputTexture = new RenderTexture(outputResolution.x, outputResolution.y, 0, RenderTextureFormat.ARGB32);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        if (outputPlane != null)
        {
            if (Application.isPlaying)
                outputPlane.material.mainTexture = outputTexture;
            else
                outputPlane.sharedMaterial.mainTexture = outputTexture;
        }
    }

    void FindCoverageCameras()
    {
        coverageCameras.Clear();

        foreach (Camera cam in Object.FindObjectsByType<Camera>(UnityEngine.FindObjectsSortMode.None))
        {
            if (cam.name.StartsWith("KinectCam"))
            {
                coverageCameras.Add(cam);
                Debug.Log($"Found coverage camera: {cam.name}");
            }

        }

        Debug.Log($"Total coverage cameras: {coverageCameras.Count}");
    }

    void Update()
    {
        if (!Application.isPlaying || voxelShader == null)
            return;

        UpdateTrackedObjects();
        UpdateVoxelGrid();
        VisualizeVoxels();
    }

    void UpdateTrackedObjects()
    {
        trackedObjectPositions.Clear();

        if (trackAllObjects)
        {
            // Track all GameObjects with colliders
            Collider[] colliders = Object.FindObjectsByType<Collider>(UnityEngine.FindObjectsSortMode.None);
            foreach (Collider col in colliders)
            {
                if (col.gameObject == outputPlane?.gameObject)
                    continue;

                Vector3 pos = col.bounds.center;

                if (pos.x >= gridMin.x && pos.x <= gridMax.x &&
                    pos.y >= gridMin.y && pos.y <= gridMax.y &&
                    pos.z >= gridMin.z && pos.z <= gridMax.z)
                {
                    trackedObjectPositions.Add(pos);
                }
            }
        }
        else
        {
            // Track only objects with specific tag
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(objectTagToTrack);
            foreach (GameObject obj in taggedObjects)
            {
                trackedObjectPositions.Add(obj.transform.position);
            }
        }
    }


    void UpdateVoxelGrid()
    {
        int kernel = voxelShader.FindKernel("UpdateVoxels");
        voxelShader.SetTexture(kernel, "VoxelGrid", voxelGrid);
        voxelShader.SetVector("gridMin", gridMin);
        voxelShader.SetVector("gridMax", gridMax);
        voxelShader.SetInts("gridResolution", gridResolution.x, gridResolution.y, gridResolution.z);
        voxelShader.SetFloat("decayRate", enablePersistence ? decayRate : 1.0f);
        voxelShader.SetFloat("deltaTime", Time.deltaTime);

        // Pass camera data
        Vector4[] camPositions = new Vector4[8];
        Vector4[] camForwards = new Vector4[8];
        for (int i = 0; i < 8; i++)
        {
            if (i < coverageCameras.Count && coverageCameras[i] != null)
            {
                camPositions[i] = coverageCameras[i].transform.position;
                camForwards[i] = coverageCameras[i].transform.forward;
            }
            else
            {
                camPositions[i] = Vector4.zero;
                camForwards[i] = new Vector4(0f, 0f, 1f, 0f);
            }
        }
        voxelShader.SetVectorArray("cameraPositions", camPositions);
        voxelShader.SetVectorArray("cameraForwards", camForwards);
        voxelShader.SetInt("numCameras", Mathf.Min(coverageCameras.Count, 8));

        // Pass object positions
        Vector3[] positions = new Vector3[100];
        for (int i = 0; i < 100; i++)
        {
            if (i < trackedObjectPositions.Count)
                positions[i] = trackedObjectPositions[i];
            else
                positions[i] = new Vector3(9999, 9999, 9999); // Far away
        }
        objectPositionsBuffer.SetData(positions);
        voxelShader.SetBuffer(kernel, "objectPositions", objectPositionsBuffer);
        voxelShader.SetInt("numObjects", trackedObjectPositions.Count);

        voxelShader.Dispatch(kernel,
            Mathf.CeilToInt(gridResolution.x / 8f),
            Mathf.CeilToInt(gridResolution.y / 8f),
            Mathf.CeilToInt(gridResolution.z / 8f));
    }

    void VisualizeVoxels()
    {
        int vizKernel = voxelShader.FindKernel("VisualizeTopDown");
        voxelShader.SetTexture(vizKernel, "VoxelGrid", voxelGrid);
        voxelShader.SetTexture(vizKernel, "Result", outputTexture);
        voxelShader.SetInts("gridResolution", gridResolution.x, gridResolution.y, gridResolution.z);

        voxelShader.Dispatch(vizKernel,
            Mathf.CeilToInt(outputResolution.x / 8f),
            Mathf.CeilToInt(outputResolution.y / 8f),
            1);
    }

    [ContextMenu("Clear Voxel Grid")]
    public void ClearGrid()
    {
        if (voxelShader != null && voxelGrid != null)
        {
            int clearKernel = voxelShader.FindKernel("ClearVoxels");
            voxelShader.SetTexture(clearKernel, "VoxelGrid", voxelGrid);
            voxelShader.Dispatch(clearKernel,
                Mathf.CeilToInt(gridResolution.x / 8f),
                Mathf.CeilToInt(gridResolution.y / 8f),
                Mathf.CeilToInt(gridResolution.z / 8f));
        }
    }

    void OnDestroy()
    {
        if (voxelGrid != null)
            voxelGrid.Release();
        if (outputTexture != null)
            outputTexture.Release();
        if (objectPositionsBuffer != null)
            objectPositionsBuffer.Release();
    }

    void OnDrawGizmos()
    {
        // Draw voxel grid bounds
        Gizmos.color = Color.cyan;
        Vector3 center = (gridMin + gridMax) / 2f;
        Vector3 size = gridMax - gridMin;
        Gizmos.DrawWireCube(center, size);

        // Draw tracked objects
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            foreach (Vector3 pos in trackedObjectPositions)
            {
                Gizmos.DrawWireSphere(pos, 0.2f);
            }
        }
    }
}