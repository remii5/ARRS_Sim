using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple 3D voxel grid storing an int count per voxel.
/// Coordinates: grid origin at 'origin' and grid axes aligned with world axes.
/// Unity units = meters.
/// </summary>
public class VoxelGrid : MonoBehaviour
{
    [Header("Grid Dimensions")]
    public Vector3 origin = Vector3.zero; // world-space origin (corner or center depending on use)
    public int sizeX = 100;
    public int sizeY = 20;
    public int sizeZ = 60;
    public float voxelSize = 0.1f; // meters

    // store counts: how many sensor hits this voxel got
    private int[,,] counts;

    public void Awake()
    {
        Allocate();
    }

    public void OnValidate()
    {
        Allocate();
    }

    public void Allocate()
    {
        counts = new int[sizeX, sizeY, sizeZ];
        Clear();
    }

    public void Clear()
    {
        if (counts == null) Allocate();
        System.Array.Clear(counts, 0, counts.Length);
    }

    /// <summary>
    /// Convert world position to grid indices. Returns false if out of bounds.
    /// Assumes origin is the minimum-corner of the grid (not center). If you want center-based, adjust before calling.
    /// </summary>
    public bool WorldToIndices(Vector3 worldPos, out int ix, out int iy, out int iz)
    {
        ix = Mathf.FloorToInt((worldPos.x - origin.x) / voxelSize);
        iy = Mathf.FloorToInt((worldPos.y - origin.y) / voxelSize);
        iz = Mathf.FloorToInt((worldPos.z - origin.z) / voxelSize);
        if (ix < 0 || iy < 0 || iz < 0 || ix >= sizeX || iy >= sizeY || iz >= sizeZ) return false;
        return true;
    }

    public void AddHitAtWorldPos(Vector3 worldPos, int increment = 1)
    {
        if (WorldToIndices(worldPos, out int ix, out int iy, out int iz))
        {
            counts[ix, iy, iz] += increment;
        }
    }

    public int GetCountAtWorldPos(Vector3 worldPos)
    {
        if (WorldToIndices(worldPos, out int ix, out int iy, out int iz))
            return counts[ix, iy, iz];
        return 0;
    }

    public int GetCountAt(int ix, int iy, int iz)
    {
        if (ix < 0 || iy < 0 || iz < 0 || ix >= sizeX || iy >= sizeY || iz >= sizeZ) return 0;
        return counts[ix, iy, iz];
    }

    // Helper: get center world position of a voxel
    public Vector3 VoxelCenterWorld(int ix, int iy, int iz)
    {
        return new Vector3(
            origin.x + (ix + 0.5f) * voxelSize,
            origin.y + (iy + 0.5f) * voxelSize,
            origin.z + (iz + 0.5f) * voxelSize
        );
    }
}
