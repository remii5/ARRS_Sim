using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class CoverageVisualizer : MonoBehaviour
{
    public VoxelGrid voxelGrid;
    public List<KinectSampler> samplers = new List<KinectSampler>();

    [Header("Sampling Control")]
    public bool autoSample = false;
    [Tooltip("Clear grid each sampling cycle")]
    public bool clearBeforeSampling = true;
    [Tooltip("If true, all samplers will perform one SampleStep per Update()")]
    public bool sampleEveryFrame = true;

    [Header("Visualization")]
    public bool showGizmos = true;
    public Mesh voxelMesh;
    public Material voxelMaterial;
    [Tooltip("Minimum camera count to highlight voxel (1 means seen by at least one camera)")]
    public int minCountToShow = 1;
    public float maxDisplayScale = 1.0f;

    void OnValidate()
    {
        if (voxelGrid != null)
        {
            // ensure voxel array allocated in edit mode
            voxelGrid.Allocate();
        }
    }

    void Update()
    {
        if (!Application.isPlaying && !autoSample) return;

        if (voxelGrid == null || samplers == null || samplers.Count == 0) return;

        if (clearBeforeSampling)
            voxelGrid.Clear();

        // either run sampling over several frames, or run a full sweep (costly)
        foreach (var s in samplers)
        {
            if (s == null) continue;
            if (sampleEveryFrame)
                s.SampleStep(); // runs samplesPerFrame rays
            else if (autoSample)
            {
                // If you want a blocking full-sweep, loop until full viewport covered.
                int iterations = (s.sampleWidth * s.sampleHeight + s.samplesPerFrame - 1) / s.samplesPerFrame;
                for (int i = 0; i < iterations; i++) s.SampleStep();
            }
        }
    }

    // Visualize voxels via gizmos or instanced mesh
    void OnDrawGizmos()
    {
        if (!showGizmos || voxelGrid == null) return;

        // Simple gizmo drawing for small grids
        int sx = voxelGrid.sizeX;
        int sy = voxelGrid.sizeY;
        int sz = voxelGrid.sizeZ;
        float vs = voxelGrid.voxelSize;

        // iterate and draw only voxels above threshold
        for (int x = 0; x < sx; x++)
        {
            for (int y = 0; y < sy; y++)
            {
                for (int z = 0; z < sz; z++)
                {
                    int count = voxelGrid.GetCountAt(x, y, z);
                    if (count >= minCountToShow)
                    {
                        Vector3 center = voxelGrid.VoxelCenterWorld(x, y, z);
                        Gizmos.color = Color.Lerp(Color.yellow, Color.red, Mathf.Clamp01((float)count / 4f));
                        Gizmos.DrawCube(center, Vector3.one * (vs * 0.95f));
                    }
                }
            }
        }
    }

    // Advanced: draw instanced mesh for many voxels (recommended for larger grids)
    public void DrawInstanced()
    {
        if (voxelGrid == null || voxelMesh == null || voxelMaterial == null) return;

        List<Matrix4x4> matrices = new List<Matrix4x4>();
        int sx = voxelGrid.sizeX, sy = voxelGrid.sizeY, sz = voxelGrid.sizeZ;
        for (int x = 0; x < sx; x++)
            for (int y = 0; y < sy; y++)
                for (int z = 0; z < sz; z++)
                {
                    int c = voxelGrid.GetCountAt(x, y, z);
                    if (c >= minCountToShow)
                    {
                        Vector3 pos = voxelGrid.VoxelCenterWorld(x, y, z);
                        float scale = voxelGrid.voxelSize * 0.9f;
                        matrices.Add(Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * scale));
                        if (matrices.Count >= 1023)
                        {
                            Graphics.DrawMeshInstanced(voxelMesh, 0, voxelMaterial, matrices);
                            matrices.Clear();
                        }
                    }
                }
        if (matrices.Count > 0)
            Graphics.DrawMeshInstanced(voxelMesh, 0, voxelMaterial, matrices);
    }
}
