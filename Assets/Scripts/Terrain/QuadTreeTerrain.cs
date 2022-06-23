using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuadTreeTerrain : MonoBehaviour
{
    public Material Material;
    public readonly List<TreeNode> Leaves = new List<TreeNode>();
    public readonly Dictionary<Vector3Int, TreeNode> Neighbors = new Dictionary<Vector3Int, TreeNode>();

    [Range(1.0f, 16000.0f)]
    public int Size = 2048;
    [Range(0.1f, 100.0f)]
    public int MinSize = 32;

    public float NoiseScale = 0.5f;
    public float NoiseHeight = 200.0f;

    public TreeNode RootNode;

    private Texture2D heightmap;
    private Mesh[] meshes = new Mesh[16];

    void Start()
    {
        RootNode = new TreeNode(this, null, transform.position, Size);

        // Generate heightmap
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(6);
        heightmap = new Texture2D(Size, Size, TextureFormat.RFloat, false);
        float[] data = new float[Size * Size];

        for (int i = 0; i < Size * Size; i++)
        {
            int x = i / Size;
            int y = i % Size;
            data[i] = noise.GetNoise(x * NoiseScale, y * NoiseScale);
        }

        heightmap.SetPixelData(data, 0, 0);
        heightmap.Apply();

        Material.SetTexture("_Heightmap", heightmap);
        Material.SetFloat("_Size", Size);
        Material.SetFloat("_Height", NoiseHeight);

        // Generate grid meshes for all possible lod neighbors
        for (byte i = 0; i < 16; i++)
        {
            BitArray bits = new BitArray(new byte[] { i });
            meshes[i] = generateGrid(32, bits[0], bits[1], bits[2], bits[3]);
        }
    }

    void Update()
    {
        // Traverse up and unload leaves that are too deep
        unloadDeepLeaves();

        // Traverse down and load new leaves
        RootNode.Update();

        // Find lod seams and update meshes
        foreach (TreeNode node in Leaves)
        {
            node.UpdateLodSeams();
            if (node.IsDirty)
            {
                node.UpdateMesh();
            }
        }
    }

    private void unloadDeepLeaves()
    {
        for (int i = 0; i < Leaves.Count; i++)
        {
            TreeNode node = Leaves[i];
            while (node.CanTraverseUp())
            {
                node.SetIsLeaf(false);
                if (node.Children != null)
                {
                    node.RemoveChildren();
                }
                node = node.Parent;
            }
        }
    }

    public void AddLeave(TreeNode node)
    {
        Leaves.Add(node);
        Neighbors.Add(node.Key, node);
    }

    public void RemoveLeave(TreeNode node)
    {
        Leaves.Remove(node);
        Neighbors.Remove(node.Key);
    }

    public Mesh GetMesh(bool[] lodSeams)
    {
        byte index = 0;
        if (lodSeams[0])
        {
            index |= 1 << 0;
        }
        if (lodSeams[1])
        {
            index |= 1 << 1;
        }
        if (lodSeams[2])
        {
            index |= 1 << 2;
        }
        if (lodSeams[3])
        {
            index |= 1 << 3;
        }
        return meshes[index];
    }

    private Mesh generateGrid(int cells, bool collapseTop, bool collapseBottom, bool collapseLeft, bool collapseRight)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(cells + 1) * (cells + 1)];

        for (int i = 0, y = 0; y <= cells; y++)
        {
            for (int x = 0; x <= cells; x++, i++)
            {
                vertices[i] = new Vector3(x, 0, y);
            }
        }

        int[] triangles = new int[cells * cells * 6];

        for (int ti = 0, vi = 0, y = 0; y < cells; y++, vi++)
        {
            for (int x = 0; x < cells; x++, ti += 6, vi++)
            {
                // Collapse x+
                if (collapseRight && x == cells - 1)
                {
                    if (y % 2 != 0)
                    {
                        if (y != cells - 1 || !collapseTop)
                        {
                            triangles[ti] = vi + cells + 1;
                            triangles[ti + 1] = vi + cells + 2;
                            triangles[ti + 2] = vi;
                        }
                        else
                        {
                            triangles[ti] = vi + cells + 0;
                            triangles[ti + 1] = vi + cells + 2;
                            triangles[ti + 2] = vi;
                        }


                        triangles[ti + 3] = vi - cells;
                        triangles[ti + 4] = vi;
                        triangles[ti + 5] = vi + cells + 2;
                    }
                    else
                    {
                        if (y != 0 || !collapseBottom)
                        {

                            triangles[ti] = vi;
                            triangles[ti + 1] = vi + cells + 1;
                            triangles[ti + 2] = vi + 1;
                        }
                        else
                        {
                            triangles[ti] = vi - 1;
                            triangles[ti + 1] = vi + cells + 1;
                            triangles[ti + 2] = vi + 1;
                        }
                    }
                }
                // Collapse x-
                else if (collapseLeft && x == 0)
                {
                    if (y % 2 != 0)
                    {
                        if (y != cells - 1 || !collapseTop)
                        {
                            triangles[ti] = vi + cells + 1;
                            triangles[ti + 1] = vi + cells + 2;
                            triangles[ti + 2] = vi + 1;
                        }

                        triangles[ti + 3] = vi + cells + 1;
                        triangles[ti + 4] = vi + 1;
                        triangles[ti + 5] = vi - cells - 1;
                    }
                    else
                    {
                        if (y != 0 || !collapseBottom)
                        {
                            triangles[ti] = vi + 2 + cells;
                            triangles[ti + 1] = vi + 1;
                            triangles[ti + 2] = vi;
                        }

                    }
                }
                // Collapse y+
                else if (collapseTop && y == cells - 1)
                {
                    if (x % 2 != 0)
                    {
                        triangles[ti] = vi + cells;
                        triangles[ti + 1] = vi + cells + 2;
                        triangles[ti + 2] = vi;

                        triangles[ti + 3] = vi + 1;
                        triangles[ti + 5] = vi + cells + 2;
                        triangles[ti + 4] = vi;
                    }
                    else
                    {
                        triangles[ti] = vi;
                        triangles[ti + 1] = vi + cells + 1;
                        triangles[ti + 2] = vi + 1;
                    }
                }
                // Collapse y-
                else if (collapseBottom && y == 0)
                {
                    if (x % 2 != 0)
                    {
                        triangles[ti] = vi + cells + 1;
                        triangles[ti + 1] = vi + cells + 2;
                        triangles[ti + 2] = vi + 1;

                        triangles[ti + 3] = vi + 1;
                        triangles[ti + 4] = vi - 1;
                        triangles[ti + 5] = vi + cells + 1;
                    }
                    else
                    {
                        triangles[ti] = vi;
                        triangles[ti + 1] = vi + cells + 1;
                        triangles[ti + 2] = vi + cells + 2;
                    }
                }
                // Default
                else
                {
                    triangles[ti + 0] = vi;
                    triangles[ti + 1] = vi + cells + 1;
                    triangles[ti + 2] = vi + 1;
                    triangles[ti + 3] = vi + 1;
                    triangles[ti + 4] = vi + cells + 1;
                    triangles[ti + 5] = vi + cells + 2;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.UploadMeshData(false);
        Bounds bounds = new Bounds();
        bounds.SetMinMax(new Vector3(0, -NoiseHeight, 0), new Vector3(cells, NoiseHeight, cells));
        mesh.bounds = bounds;
        return mesh;
    }

    private void OnDrawGizmos()
    {
        if (RootNode != null)
        {
            foreach (TreeNode leaf in Leaves)
            {
                leaf.DrawDebug();
            }
        }
    }
}

