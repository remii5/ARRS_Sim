using UnityEngine;

public class SimpleVoxelTest : MonoBehaviour
{
    [Header("Visualization")]
    public Renderer outputPlane;
    public ComputeShader voxelShader;
    public Vector2Int outputResolution = new Vector2Int(512, 512);

    [Header("Test Settings")]
    public bool showTestPattern = true;

    private RenderTexture outputTexture;

    void Start()
    {
        SetupOutput();

        if (showTestPattern)
            DrawTestPattern();
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

    void DrawTestPattern()
    {
        if (voxelShader == null)
        {
            Debug.LogError("Voxel shader not assigned!");
            return;
        }

        // Just draw a simple test pattern to verify the shader works
        int kernel = voxelShader.FindKernel("TestPattern");
        if (kernel < 0)
        {
            Debug.LogError("TestPattern kernel not found in compute shader!");
            return;
        }

        voxelShader.SetTexture(kernel, "Result", outputTexture);
        voxelShader.SetFloat("time", Time.time);

        voxelShader.Dispatch(kernel,
            Mathf.CeilToInt(outputResolution.x / 8f),
            Mathf.CeilToInt(outputResolution.y / 8f),
            1);

        Debug.Log("Test pattern drawn!");
    }

    void Update()
    {
        if (showTestPattern && Application.isPlaying)
            DrawTestPattern();
    }

    void OnDestroy()
    {
        if (outputTexture != null)
            outputTexture.Release();
    }
}