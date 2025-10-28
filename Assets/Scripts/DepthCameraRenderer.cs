using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DepthCameraRenderer : MonoBehaviour
{
    public RenderTexture depthTexture;
    private Camera cam;
    private Material depthMaterial;

    public void InitializeDepthTexture(int width, int height)
    {
        cam = GetComponent<Camera>();

        if (depthTexture != null)
            depthTexture.Release();

        // Create depth texture
        depthTexture = new RenderTexture(width, height, 24, RenderTextureFormat.RFloat);
        depthTexture.filterMode = FilterMode.Point;
        depthTexture.Create();

        // Create depth material
        if (depthMaterial == null)
        {
            Shader depthShader = Shader.Find("Hidden/DepthOnly");
            if (depthShader == null)
            {
                // Create inline depth shader if not found
                depthShader = CreateDepthShader();
            }
            depthMaterial = new Material(depthShader);
        }

        // Don't set targetTexture - we'll render manually
        cam.enabled = true; // Ensure camera is enabled
    }

    private Shader CreateDepthShader()
    {
        // Attempt to find a shader; fallback to "Hidden/DepthOnly"
        Shader shader = Shader.Find("Custom/RenderDepth");
        if (shader == null)
        {
            shader = Shader.Find("Hidden/DepthOnly");
        }
        return shader;
    }

    void LateUpdate()
    {
        // Manually render depth each frame
        if (depthTexture != null && depthMaterial != null && cam != null)
        {
            RenderTexture currentRT = RenderTexture.active;

            // Render to depth texture with replacement shader
            cam.targetTexture = depthTexture;
            cam.RenderWithShader(depthMaterial.shader, "RenderType");
            cam.targetTexture = null;

            RenderTexture.active = currentRT;
        }
    }

    void OnDestroy()
    {
        if (depthTexture != null)
        {
            depthTexture.Release();
            depthTexture = null;
        }

        if (depthMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(depthMaterial);
            else
                DestroyImmediate(depthMaterial);
        }
    }
}