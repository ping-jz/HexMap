using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Video;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    Mesh hexMesh;
    List<Vector3> vertices;
    List<int> triangles;
    List<Color> colors;
    MeshCollider meshCollider;

    [System.Flags]
    public enum GizmoMode { Nothing = 0, Triangles = 0b0001 }

    [SerializeField]
    GizmoMode gizmos;

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();

        hexMesh.name = "Hex Mesh";
        vertices = new List<Vector3>();
        triangles = new List<int>();
        colors = new List<Color>();
    }

    public void Triangulate(HexCell[] cells)
    {
        hexMesh.Clear();
        vertices.Clear();
        triangles.Clear();
        colors.Clear();
        for (int i = 0; i < cells.Length; i++)
        {
            TriangulateCell(cells[i]);
        }

        hexMesh.vertices = vertices.ToArray();
        hexMesh.triangles = triangles.ToArray();
        hexMesh.colors = colors.ToArray();
        hexMesh.RecalculateNormals();
        meshCollider.sharedMesh = hexMesh;
    }

    void TriangulateCell(HexCell cell)
    {
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            TriangulateCell(d, cell);
        }
    }


    void TriangulateCell(HexDirection direction, HexCell cell)
    {
        Vector3 cetner = cell.transform.localPosition;
        Vector3 v1 = cetner + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 v2 = cetner + HexMetrics.GetSecondSolidCorner(direction);
        AddTriangle(cetner, v1, v2);
        AddTriangleColor(cell.color);

        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, v1, v2);
        }
    }

    private void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
        {
            return;
        }

        Vector3 bridge = HexMetrics.GetBridge(direction);
        Vector3 v3 = v1 + bridge;
        Vector3 v4 = v2 + bridge;
        v3.y = v4.y = neighbor.Elevation * HexMetrics.elevationStep;

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(v1, v2, cell, v3, v4, neighbor);
        }
        else
        {
            AddQuad(v1, v2, v3, v4);
            Color bridgeColor = (cell.color + neighbor.color) * 0.5f;
            AddQuadColor(cell.color, bridgeColor);
        }

        HexCell next = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && next != null)
        {
            Color bridgeColor = (cell.color + neighbor.color) * 0.5f;
            Vector3 v5 = v2 + HexMetrics.GetBridge(direction.Next());
            v5.y = next.Elevation * HexMetrics.elevationStep;
            AddTriangle(v2, v4, v5);
            AddTriangleColor(
                cell.color,
                bridgeColor,
                (cell.color + next.color + neighbor.color) / 3f
            );
        }

    }

    private void TriangulateEdgeTerraces(
        Vector3 beginLeft, Vector3 beginRight, HexCell beginCell,
        Vector3 endLeft, Vector3 endRight, HexCell endCell)
    {
        Vector3 v3 = beginLeft;
        Vector3 v4 = beginRight;
        Color c2 = beginCell.color;
        for (int i = 1; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, i);
            v4 = HexMetrics.TerraceLerp(beginRight, endRight, i);
            c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(
              beginCell.color,
              c2
            );
        }

        AddQuad(v3, v4, endLeft, endRight);
        AddQuadColor(
          c2,
          endCell.color
        );
    }

    void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    void AddTriangleColor(Color c1)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c1);
    }

    void AddTriangleColor(Color c1, Color c2, Color c3)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }

    void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
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

    void AddQuadColor(Color c1, Color c2)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }


    void OnDrawGizmos()
    {
        if (gizmos == GizmoMode.Nothing || triangles == null)
        {
            return;
        }

        bool drawTiangles = (gizmos & GizmoMode.Triangles) != 0;

        if (drawTiangles)
        {
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Gizmos.DrawLine(vertices[triangles[i]], vertices[triangles[i + 1]]);
                Gizmos.DrawLine(vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
                Gizmos.DrawLine(vertices[triangles[i + 2]], vertices[triangles[i]]);
            }
        }

    }
}