using UnityEngine;
using System.Collections.Generic;
using System;

[Flags]
public enum MeshFlags
{
    Nothing = 0,
    Collider = 0b00001,
    CellData = 0b00010,
    UVCoordinates = 0b00100,
    UVCoordinates1 = 0b01000,
}

public static class MeshFlagsExtensions
{
    public static bool Has(this MeshFlags flags, MeshFlags mask) =>
        (flags & mask) == mask;

    public static bool HasAny(this MeshFlags flags, MeshFlags mask) =>
        (flags & mask) != 0;
    public static bool HasNot(this MeshFlags flags, MeshFlags mask) =>
        (flags & mask) != mask;


    public static MeshFlags With(this MeshFlags flags, MeshFlags mask) =>
        flags | mask;
    public static MeshFlags Without(this MeshFlags flags, MeshFlags mask) =>
        flags & ~mask;
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    [NonSerialized] List<Vector3> vertices, cellIndices;
    [NonSerialized] List<Color> cellWeights;
    [NonSerialized] List<Vector2> uvs, uv1s;
    [NonSerialized] List<int> triangles;

    [SerializeField]
    private MeshFlags flags;
    Mesh hexMesh;
    MeshCollider meshCollider;

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        if (flags.Has(MeshFlags.Collider))
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        hexMesh.name = "Hex Mesh";
    }

    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleColor(Color c1)
    {
        cellWeights.Add(c1);
        cellWeights.Add(c1);
        cellWeights.Add(c1);
    }

    public void AddTriangleColor(Color c1, Color c2, Color c3)
    {
        cellWeights.Add(c1);
        cellWeights.Add(c2);
        cellWeights.Add(c3);
    }

    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        //v3       v4
        //  v1  v2
        // v1 v3 v2 组成三角形
        // v2 v3 v4 组成三角形
        int vertexIndex = vertices.Count;
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
        vertices.Add(HexMetrics.Perturb(v4));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        //v3       v4
        //  v1  v2
        // v1 v3 v2 组成三角形
        // v2 v3 v4 组成三角形
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }


    public void AddQuadColor(Color color)
    {
        cellWeights.Add(color);
        cellWeights.Add(color);
        cellWeights.Add(color);
        cellWeights.Add(color);
    }

    public void AddQuadColor(Color c1, Color c2)
    {
        cellWeights.Add(c1);
        cellWeights.Add(c1);
        cellWeights.Add(c2);
        cellWeights.Add(c2);
    }

    public void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        cellWeights.Add(c1);
        cellWeights.Add(c2);
        cellWeights.Add(c3);
        cellWeights.Add(c4);
    }

    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
    }

    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
        uvs.Add(uv4);
    }

    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        uvs.Add(new Vector2(uMin, vMin));
        uvs.Add(new Vector2(uMax, vMin));
        uvs.Add(new Vector2(uMin, vMax));
        uvs.Add(new Vector2(uMax, vMax));
    }

    public void AddTriangleUV1(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        uv1s.Add(uv1);
        uv1s.Add(uv2);
        uv1s.Add(uv3);
    }

    public void AddQuadUV1(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        uv1s.Add(uv1);
        uv1s.Add(uv2);
        uv1s.Add(uv3);
        uv1s.Add(uv4);
    }

    public void AddQuadUV1(float uMin, float uMax, float vMin, float vMax)
    {
        uv1s.Add(new Vector2(uMin, vMin));
        uv1s.Add(new Vector2(uMax, vMin));
        uv1s.Add(new Vector2(uMin, vMax));
        uv1s.Add(new Vector2(uMax, vMax));
    }

    public void AddTriangleCellData(
      Vector3 indices, Color weights1, Color weights2, Color weights3
  )
    {
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellWeights.Add(weights1);
        cellWeights.Add(weights2);
        cellWeights.Add(weights3);
    }

    public void AddTriangleCellData(Vector3 indices, Color weights)
    {
        AddTriangleCellData(indices, weights, weights, weights);
    }

    public void AddQuadCellData(
    Vector3 indices,
    Color weights1, Color weights2, Color weights3, Color weights4
    )
    {
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellWeights.Add(weights1);
        cellWeights.Add(weights2);
        cellWeights.Add(weights3);
        cellWeights.Add(weights4);
    }

    public void AddQuadCellData(
        Vector3 indices, Color weights1, Color weights2
    )
    {
        AddQuadCellData(indices, weights1, weights1, weights2, weights2);
    }

    public void AddQuadCellData(Vector3 indices, Color weights)
    {
        AddQuadCellData(indices, weights, weights, weights, weights);
    }


    public void Clear()
    {
        hexMesh.Clear();
        vertices = ListPool<Vector3>.Get();
        triangles = ListPool<int>.Get();
        if (flags.Has(MeshFlags.CellData))
        {
            cellWeights = ListPool<Color>.Get();
            cellIndices = ListPool<Vector3>.Get();
        }
        if (flags.Has(MeshFlags.UVCoordinates))
        {
            uvs = ListPool<Vector2>.Get();
        }
        if (flags.Has(MeshFlags.UVCoordinates1))
        {
            uv1s = ListPool<Vector2>.Get();
        }
    }

    public void Apply()
    {

        hexMesh.SetVertices(vertices);
        ListPool<Vector3>.Add(vertices);
        hexMesh.SetTriangles(triangles, 0);
        ListPool<int>.Add(triangles);
        if (flags.Has(MeshFlags.CellData))
        {
            hexMesh.SetColors(cellWeights);
            ListPool<Color>.Add(cellWeights);
            hexMesh.SetUVs(2, cellIndices);
            ListPool<Vector3>.Add(cellIndices);
        }

        hexMesh.RecalculateNormals();
        if (flags.Has(MeshFlags.Collider))
        {
            meshCollider.sharedMesh = hexMesh;
        }

        if (flags.Has(MeshFlags.UVCoordinates))
        {
            hexMesh.SetUVs(0, uvs);
            ListPool<Vector2>.Add(uvs);
        }

        if (flags.Has(MeshFlags.UVCoordinates1))
        {
            hexMesh.SetUVs(1, uv1s);
            ListPool<Vector2>.Add(uv1s);
        }
    }

    public void DrawGizmos()
    {
        List<Vector3> vertices = ListPool<Vector3>.Get();
        int[] triangles = hexMesh.GetTriangles(0);
        hexMesh.GetVertices(vertices);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Gizmos.DrawLine(vertices[triangles[i]], vertices[triangles[i + 1]]);
            Gizmos.DrawLine(vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
            Gizmos.DrawLine(vertices[triangles[i + 2]], vertices[triangles[i]]);
        }
        ListPool<Vector3>.Add(vertices);
    }

}
