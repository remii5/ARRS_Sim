using UnityEngine;

/// <summary>
/// Attach this to each Kinect camera GameObject (with a Camera component).
/// It samples the camera frustum by creating a viewport grid (nx × ny), casting rays,
/// and writing hit points into a shared VoxelGrid.
/// </summary>
[RequireComponent(typeof(Camera))]
public class KinectSampler : MonoBehaviour
{
    public Camera cam;
    public VoxelGrid voxelGrid; // assign manager in inspector
    [Tooltip("Viewport sampling resolution (width × height). Lower = faster, coarser.")]
    public int sampleWidth = 160;  // e.g. 160x120 is coarse but reasonable
    public int sampleHeight = 120;
    [Tooltip("Maximum sampling distance (should match camera.farClipPlane).")]
    public float maxDistance = 2.21f;
    [Tooltip("Optional layer mask for sampling (only consider colliders on these layers)")]
    public LayerMask samplingMask = ~0; // default everything
    [Tooltip("Whether to use exact raycast hit point or approximate depth (first hit).")]
    public bool useRaycast = true;

    [Header("Performance")]
    [Tooltip("Samples per frame; set lower to spread sampling across frames.")]
    public int samplesPerFrame = 2000;

    private int currentU = 0;
    private int currentV = 0;
    private int totalSamplesPerViewport;

    void Reset()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (cam == null) cam = GetComponent<Camera>();
        totalSamplesPerViewport = sampleWidth * sampleHeight;
    }

    // call this externally from manager to reset sampling
    public void ResetSampler()
    {
        currentU = 0; currentV = 0;
    }

    /// <summary>
    /// Run a sampling "step". Can be called every frame or in a coroutine.
    /// </summary>
    public void SampleStep()
    {
        if (voxelGrid == null || cam == null) return;

        int samples = Mathf.Max(1, samplesPerFrame);
        for (int s = 0; s < samples; s++)
        {
            // compute viewport coordinates (u,v) in [0,1]
            float u = (currentU + 0.5f) / sampleWidth;
            float v = (currentV + 0.5f) / sampleHeight;

            Ray ray = cam.ViewportPointToRay(new Vector3(u, v, 0f));

            if (useRaycast)
            {
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Min(maxDistance, cam.farClipPlane), samplingMask))
                {
                    voxelGrid.AddHitAtWorldPos(hit.point);
                }
            }
            else
            {
                // approximate: place point at camera forward * cam.farClipPlane
                Vector3 pt = ray.origin + ray.direction * Mathf.Min(maxDistance, cam.farClipPlane);
                // You could also check for overlap with colliders via Physics.OverlapSphere if desired.
                voxelGrid.AddHitAtWorldPos(pt);
            }

            // advance sample index
            currentU++;
            if (currentU >= sampleWidth)
            {
                currentU = 0;
                currentV++;
                if (currentV >= sampleHeight)
                    currentV = 0; // wrap to start (one full pass)
            }
        }
    }
}
