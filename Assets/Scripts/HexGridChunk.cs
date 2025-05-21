using System;
using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    [SerializeField]
    private HexMesh terrain, rivers, roads, water, waterShore;
    HexCell[] cells;
    Canvas gridCanvas;

    void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
    }

    public void AddCell(int index, HexCell cell)
    {
        cells[index] = cell;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }

    public void Refresh()
    {
        enabled = true;
    }

    void LateUpdate()
    {
        Triangulate(cells);
        enabled = false;
    }

    public void Triangulate(HexCell[] cells)
    {
        terrain.Clear();
        rivers.Clear();
        roads.Clear();
        water.Clear();
        waterShore.Clear();
        foreach (HexCell cell in cells)
        {
            TriangulateCell(cell);
        }
        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
    }

    void TriangulateCell(HexCell cell)
    {
        for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
        {
            TriangulateCell(d, cell);
        }

    }

    void TriangulateCell(HexDirection direction, HexCell cell)
    {
        Vector3 center = cell.Position;
        EdgeVertices e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(direction))
            {
                e.v3.y = cell.StreamBedY;
                if (cell.HasRiverBeginOrEnd)
                {
                    TriangulateCellWithRiverBeginOrEnd(direction, cell, center, e);
                }
                else
                {
                    TriangulateCellWithRiver(direction, cell, center, e);
                }
            }
            else
            {
                TriangulateAdjacentToRiver(direction, cell, center, e);
            }
        }
        else
        {
            TriangulateWithoutRiver(direction, cell, center, e);
        }


        if (direction <= HexDirection.BottomRight)
        {
            TriangulateConnection(direction, cell, e);
        }

        if (cell.IsUnderwater)
        {
            TriangulateWater(direction, cell, center);
        }
    }

    void TriangulateWater(HexDirection direction, HexCell cell, Vector3 center)
    {
        center.y = cell.WaterSurfaceY;
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor != null && !neighbor.IsUnderwater)
        {
            TriangulateWaterShore(direction, cell, neighbor, center);
        }
        else
        {
            TriangulateOpenWater(direction, cell, neighbor, center);
        }
    }

    private void TriangulateWaterShore(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {
        EdgeVertices e1 = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        water.AddTriangle(center, e1.v1, e1.v2);
        water.AddTriangle(center, e1.v2, e1.v3);
        water.AddTriangle(center, e1.v3, e1.v4);
        water.AddTriangle(center, e1.v4, e1.v5);

        Vector3 bridege = HexMetrics.GetBridge(direction);
        EdgeVertices e2 = new EdgeVertices(
            e1.v1 + bridege,
            e1.v5 + bridege
        );
        waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        waterShore.AddQuadUV(0f, 0f, 0f, 1f);
        waterShore.AddQuadUV(0f, 0f, 0f, 1f);
        waterShore.AddQuadUV(0f, 0f, 0f, 1f);
        waterShore.AddQuadUV(0f, 0f, 0f, 1f);

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (nextNeighbor != null)
        {
            waterShore.AddTriangle(e1.v5, e2.v5, e1.v5 + HexMetrics.GetBridge(direction.Next()));
            waterShore.AddTriangleUV(
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f)
            );
        }
    }

    private void TriangulateOpenWater(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {
        Vector3 c1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondSolidCorner(direction);
        water.AddTriangle(center, c1, c2);

        if (direction <= HexDirection.BottomRight && neighbor != null)
        {
            Vector3 bridge = HexMetrics.GetBridge(direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;
            water.AddQuad(c1, c2, e1, e2);

            if (direction <= HexDirection.Right)
            {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater)
                {
                    return;
                }

                water.AddTriangle(c2, e2, c2 + HexMetrics.GetBridge(direction.Next()));
            }
        }

        return;
    }

    void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        if (cell.HasRoads)
        {
            TriangulateRoadAdjacentToRiver(direction, cell, center, e);
        }

        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                center += HexMetrics.GetSolidEdgeMiddle(direction) *
                    (HexMetrics.innerToOuter * 0.5f);
            }
            else if (
                cell.HasRiverThroughEdge(direction.Previous2())
            )
            {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (
            cell.HasRiverThroughEdge(direction.Previous()) &&
            cell.HasRiverThroughEdge(direction.Next2())
        )
        {
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }

        EdgeVertices m = new EdgeVertices(
                    Vector3.Lerp(center, e.v1, 0.5f),
                    Vector3.Lerp(center, e.v5, 0.5f)
                );

        TriangulateEdgeStrip(m, cell.color, e, cell.color);
        TriangulateEdgeFan(center, m, cell.color);
    }

    void TriangulateRoadAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
        bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
        bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());
        Vector2 interpolators = GetRoadInterpolators(direction, cell);
        Vector3 roadCenter = center;
        if (cell.HasRiverBeginOrEnd)
        {
            roadCenter += HexMetrics
                .GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite())
                * (1f / 3f);
        }
        else if (HasStraightRiver(cell))
        {
            Vector3 corner;
            if (previousHasRiver)
            {
                corner = HexMetrics.GetSecondSolidCorner(direction);
            }
            else
            {
                corner = HexMetrics.GetFirstSolidCorner(direction);
            }
            roadCenter += corner * 0.5f;
            center += corner * 0.25f;
        }
        else if (HasZigzagRiverPrev(cell))
        {
            roadCenter -= HexMetrics.GetSecondCorner(cell.Flags.RiverInToDirection()) * 0.25f;
        }
        else if (HasZigzagRiverNext(cell))
        {
            roadCenter -= HexMetrics.GetFirstCorner(cell.Flags.RiverInToDirection()) * 0.25f;
        }
        //这时候就很体现对问题的描述能力了
        else if (previousHasRiver && nextHasRiver)
        {
            if (!hasRoadThroughEdge)
            {
                return;
            }
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) *
                        HexMetrics.innerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        }
        else
        {
            //太多小细节了，先知道吧。不纠结了
            HexDirection middle;
            if (previousHasRiver)
            {
                middle = direction.Next();
            }
            else if (nextHasRiver)
            {
                middle = direction.Previous();
            }
            else
            {
                middle = direction;
            }
            roadCenter += HexMetrics.GetSolidEdgeMiddle(middle) * 0.25f;
        }

        Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
        if (previousHasRiver)
        {
            if (
                    !hasRoadThroughEdge &&
                    !cell.HasRoadThroughEdge(direction.Next())
                )
            {
                return;
            }
            TriangulateRoadEdge(roadCenter, center, mL);
        }
        if (nextHasRiver)
        {
            if (
                !hasRoadThroughEdge &&
                !cell.HasRoadThroughEdge(direction.Previous())
            )
            {
                return;
            }
            TriangulateRoadEdge(roadCenter, mR, center);
        }

        if (hasRoadThroughEdge)
        {
            TriangulateRoad(roadCenter, mL, mR, e);
        }
        else
        {
            TriangulateRoadEdge(roadCenter, mL, mR);
        }
    }

    private bool HasStraightRiver(HexCell cell)
    {
        HexCellFlags flags = cell.Flags;

        if (!flags.HasAny(HexCellFlags.RiverIn) ||
        !flags.HasAny(HexCellFlags.RiverOut))
        {
            return false;
        }

        return flags.RiverInToDirection() == flags.RiverOutToDirection().Opposite();
    }


    private bool HasZigzagRiverPrev(HexCell cell)
    {
        HexCellFlags flags = cell.Flags;

        if (!flags.HasAny(HexCellFlags.RiverIn) ||
        !flags.HasAny(HexCellFlags.RiverOut))
        {
            return false;
        }

        return flags.RiverInToDirection() == flags.RiverOutToDirection().Previous();
    }

    private bool HasZigzagRiverNext(HexCell cell)
    {
        HexCellFlags flags = cell.Flags;

        if (!flags.HasAny(HexCellFlags.RiverIn) ||
        !flags.HasAny(HexCellFlags.RiverOut))
        {
            return false;
        }

        return flags.RiverInToDirection() == flags.RiverOutToDirection().Next();
    }

    void TriangulateCellWithRiverBeginOrEnd(
        HexDirection direction,
        HexCell cell,
        Vector3 center,
        EdgeVertices e)
    {
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );
        m.v3.y = e.v3.y;

        TriangulateEdgeStrip(m, cell.color, e, cell.color);
        TriangulateEdgeFan(center, m, cell.color);

        bool reversed = cell.Flags.HasAny(HexCellFlags.RiverIn);
        TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
        center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
        rivers.AddTriangle(center, m.v2, m.v4);
        //这段代码没看懂，为什么是1/2,UV这部分差太多基础只是了。有点难理解
        if (reversed)
        {
            rivers.AddTriangleUV(
                new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f)
            );
        }
        else
        {
            rivers.AddTriangleUV(
                new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f)
            );
        }
    }

    void TriangulateCellWithRiver(
        HexDirection direction,
        HexCell cell,
        Vector3 center,
        EdgeVertices e)
    {
        Vector3 centerL, centerR;
        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;

        }
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {

            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            centerL = center;
            centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.innerToOuter);
        }
        else
        {
            centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
            centerR = center;
        }
        center = Vector3.Lerp(centerL, centerR, 0.5f);

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(centerL, e.v1, 0.5f),
            Vector3.Lerp(centerR, e.v5, 0.5f),
            1f / 6f
        );

        m.v3.y = center.y = e.v3.y;
        TriangulateEdgeStrip(m, cell.color, e, cell.color);
        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddTriangleColor(cell.color);

        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuadColor(cell.color);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddQuadColor(cell.color);

        terrain.AddTriangle(centerR, m.v4, m.v5);
        terrain.AddTriangleColor(cell.color);

        bool reversed = cell.Flags.HasRiverIn(direction);
        TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed);
        TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
    }

    private void TriangulateConnection(
        HexDirection direction, HexCell cell,
        EdgeVertices e1)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
        {
            return;
        }

        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighbor.Position.y - cell.Position.y;
        EdgeVertices e2 = new EdgeVertices(e1.v1 + bridge, e1.v5 + bridge);

        if (cell.HasRiverThroughEdge(direction))
        {
            e2.v3.y = neighbor.StreamBedY;
            TriangulateRiverQuad(e1.v2, e1.v4, e2.v2, e2.v4,
            cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
            cell.Flags.HasRiverIn(direction));
        }

        bool hasRoad = cell.HasRoadThroughEdge(direction);
        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            if (hasRoad)
            {
                TriangulateEdgeTerracesRoad(e1, cell, e2, neighbor);
            }
            else
            {
                TriangulateEdgeTerraces(e1, cell, e2, neighbor);
            }
        }
        else
        {
            if (hasRoad)
            {
                TriangulateEdgeStripRoad(e1, cell.color, e2, neighbor.color);
            }
            else
            {
                TriangulateEdgeStrip(e1, cell.color, e2, neighbor.color);
            }

        }

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.Right && nextNeighbor != null)
        {
            Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;

            //work from bottom to left then to right
            //left     right
            //   bottom
            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor);
                }
                else
                {
                    TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell);
            }
            else
            {
                TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
            }
        }

    }

    private void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerpColor(beginCell.color, endCell.color, 1);
        TriangulateEdgeStrip(begin, beginCell.color, e2, c2);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerpColor(beginCell.color, endCell.color, i);
            TriangulateEdgeStrip(e1, c1, e2, c2);
        }

        TriangulateEdgeStrip(e2, c2, end, endCell.color);
    }

    private void TriangulateEdgeTerracesRoad(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerpColor(beginCell.color, endCell.color, 1);
        TriangulateEdgeStripRoad(begin, beginCell.color, e2, c2);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerpColor(beginCell.color, endCell.color, i);
            TriangulateEdgeStripRoad(e1, c1, e2, c2);
        }

        TriangulateEdgeStripRoad(e2, c2, end, endCell.color);
    }

    void TriangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            switch (rightEdgeType)
            {
                case HexEdgeType.Slope:
                    {
                        TriangulateCornerTerraces(
                            bottom, bottomCell,
                            left, leftCell,
                            right, rightCell
                        );
                        return;
                    }
                case HexEdgeType.Flat:
                    {
                        TriangulateCornerTerraces(
                            left, leftCell,
                            right, rightCell,
                            bottom, bottomCell
                        );
                        return;
                    }
                case HexEdgeType.Cliff:
                    {
                        TriangulateCornerTerracesCliff(
                            bottom, bottomCell,
                            left, leftCell,
                            right, rightCell
                        );
                        return;
                    }
                default:
                    throw new ArgumentOutOfRangeException(
                    nameof(rightEdgeType),
                    $"Not expected HexEdgeType value: {rightEdgeType}"
                );
            }
        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(
                   right, rightCell,
                   bottom, bottomCell,
                   left, leftCell
               );
            }
            else if (leftEdgeType == HexEdgeType.Cliff)
            {
                TriangulateCornerCliffTerraces(
                    bottom, bottomCell,
                    left, leftCell,
                    right, rightCell
                );
            }
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
            }

        }
        else
        {
            terrain.AddTriangle(bottom, left, right);
            terrain.AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
        }
    }

    void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerpColor(beginCell.color, leftCell.color, 1);
        Color c4 = HexMetrics.TerraceLerpColor(beginCell.color, rightCell.color, 1);
        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleColor(beginCell.color, c3, c4);
        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerpColor(beginCell.color, leftCell.color, i);
            c4 = HexMetrics.TerraceLerpColor(beginCell.color, rightCell.color, i);
            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadColor(c1, c2, c3, c4);
        }

        terrain.AddQuad(v3, v4, left, right);
        terrain.AddQuadColor(
           c3,
           c4,
           leftCell.color,
           rightCell.color
         );
    }

    void TriangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        //数学真神奇
        float b = Mathf.Abs(1f / (rightCell.Elevation - beginCell.Elevation));
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        Color boundaryColor = Color.Lerp(beginCell.color, rightCell.color, b);

        TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            terrain.AddTriangleUnPerturb(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    void TriangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        float b = Mathf.Abs(1f / (leftCell.Elevation - beginCell.Elevation));

        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        Color boundaryColor = Color.Lerp(beginCell.color, leftCell.color, b);

        TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            terrain.AddTriangleUnPerturb(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    private void TriangulateBoundaryTriangle(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor
    )
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerpColor(beginCell.color, leftCell.color, 1);
        terrain.AddTriangleUnPerturb(HexMetrics.Perturb(begin), v2, boundary);
        terrain.AddTriangleColor(beginCell.color, c2, boundaryColor);
        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerpColor(beginCell.color, leftCell.color, i);
            terrain.AddTriangleUnPerturb(v1, v2, boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);
        }

        terrain.AddTriangleUnPerturb(v2, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleColor(c2, leftCell.color, boundaryColor);
    }

    void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y, float v, bool reversed
        )
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
    }

    void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float v, bool reversed
        )
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        rivers.AddQuad(v1, v2, v3, v4);
        if (reversed)
        {
            rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
        }
        else
        {
            rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
        }
    }

    void TriangulateWithoutRiver(
        HexDirection direction, HexCell cell,
        Vector3 center, EdgeVertices e)
    {
        TriangulateEdgeFan(center, e, cell.color);
        if (cell.HasRoads)
        {
            Vector2 interpolators = GetRoadInterpolators(direction, cell);
            Vector3 mL = Vector3.Lerp(center, e.v1, interpolators.x);
            Vector3 mR = Vector3.Lerp(center, e.v5, interpolators.y);
            if (cell.HasRoadThroughEdge(direction))
            {
                TriangulateRoad(
                    center,
                    mL,
                    mR,
                    e
                );
            }
            else
            {
                TriangulateRoadEdge(center, mL, mR);
            }
        }
    }

    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v4, edge.v5);
        terrain.AddTriangleColor(color);
    }

    void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2)
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        terrain.AddQuadColor(c1, c2);
    }
    void TriangulateEdgeStripRoad(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2)
    {
        TriangulateEdgeStrip(e1, c1, e2, c2);
        TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4);
    }

    void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e)
    {
        Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
        TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4);
        roads.AddTriangle(center, mL, mC);
        roads.AddTriangle(center, mC, mR);
        roads.AddTriangleUV(
            new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f)
        );
        roads.AddTriangleUV(
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f)
        );
    }

    void TriangulateRoadSegment(
        Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 v4, Vector3 v5, Vector3 v6
    )
    {
        roads.AddQuad(v1, v2, v4, v5);
        roads.AddQuad(v2, v3, v5, v6);
        roads.AddQuadUV(0f, 1f, 0f, 0f);
        roads.AddQuadUV(1f, 0f, 0f, 0f);
    }

    void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR)
    {
        roads.AddTriangle(center, mL, mR);
        roads.AddTriangleUV(
            new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
    }

    Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
    {
        Vector2 interpolators;
        if (cell.HasRoadThroughEdge(direction))
        {
            interpolators = new Vector2(0.5f, 0.5f);
        }
        else
        {
            interpolators.x =
            cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
            interpolators.y =
                cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
        }
        return interpolators;
    }

    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }

    public void DrawGizmos(GizmoMode gizmoMode)
    {
        if (gizmoMode.HasFlag(GizmoMode.Terrian))
        {
            Gizmos.color = Color.red;
            terrain.DrawGizmos();
        }
        if (gizmoMode.HasFlag(GizmoMode.River))
        {
            Gizmos.color = Color.blue;
            rivers.DrawGizmos();
            water.DrawGizmos();
            waterShore.DrawGizmos();
        }
        if (gizmoMode.HasFlag(GizmoMode.Road))
        {
            Gizmos.color = Color.green;
            roads.DrawGizmos();
        }
    }

}