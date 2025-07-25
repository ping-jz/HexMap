using System;
using UnityEngine;

public class HexGridChunk : MonoBehaviour
{

    static Color weights1 = new Color(1f, 0f, 0f);
    static Color weights2 = new Color(0f, 1f, 0f);
    static Color weights3 = new Color(0f, 0f, 1f);

    [SerializeField]
    private HexMesh terrain, rivers, roads, water, waterShore, estuaries;
    [SerializeField]
    private HexFeatureManager features;
    int[] cellsIndices;
    Canvas gridCanvas;

    public HexGrid Grid { get; set; }

    void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        cellsIndices = new int[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
    }

    public void AddCell(int index, HexCell cell)
    {
        cellsIndices[index] = cell.Index;
        Grid.UiRects[index].rectTransform.SetParent(gridCanvas.transform, false);
    }

    public void Refresh()
    {
        enabled = true;
    }

    void LateUpdate()
    {
        Triangulate(cellsIndices);
        enabled = false;
    }

    public void Triangulate(int[] cells)
    {
        terrain.Clear();
        rivers.Clear();
        roads.Clear();
        water.Clear();
        waterShore.Clear();
        estuaries.Clear();
        features.Clear();
        foreach (int cell in cells)
        {
            Triangulate(cell);
        }
        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
        estuaries.Apply();
        features.Apply();
    }

    void Triangulate(int cellIdx)
    {
        HexCellData cell = Grid.CellData[cellIdx];
        Vector3 cellPosition = Grid.CellPositions[cellIdx];
        for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
        {
            Triangulate(d, cell, cellIdx, cellPosition);
        }
        if (!cell.IsUnderwater && !cell.HasRiver && !cell.HasRoads)
        {
            features.AddFeature(cell, cellPosition);
        }
        if (cell.IsSpecial)
        {
            features.AddSpecialFeature(cell, cellPosition);
        }
    }

    void Triangulate(HexDirection direction, HexCellData cell, int cellIdx, Vector3 cellPosition)
    {
        Vector3 center = cellPosition;
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
                    TriangulateWithRiverBeginOrEnd(cell, cellIdx, center, e);
                }
                else
                {
                    TriangulateWithRiver(direction, cell, cellIdx, center, e);
                }
            }
            else
            {
                TriangulateAdjacentToRiver(direction, cell, cellIdx, center, e);
            }
        }
        else
        {
            TriangulateWithoutRiver(direction, cell, cellIdx, center, e);

            if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
            {
                features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
            }
        }


        if (direction <= HexDirection.BottomRight)
        {
            TriangulateConnection(direction, cell, cellIdx, center.y, e);
        }

        if (cell.IsUnderwater)
        {
            TriangulateWater(direction, cell, cellIdx, center);
        }
    }

    void TriangulateWater(HexDirection direction, HexCellData cell, int cellIndex, Vector3 center)
    {
        center.y = cell.WaterSurfaceY;
        HexCoordinates neighborCoordinates = cell.coordinates.Step(direction);
        if (Grid.GetCellIdx(neighborCoordinates, out int neighbor) && !Grid.CellData[neighbor].IsUnderwater)
        {
            TriangulateWaterShore(direction, cell, cellIndex, neighbor, neighborCoordinates.ColumnIndex, center);
        }
        else
        {
            TriangulateOpenWater(cell.coordinates, direction, cellIndex, neighbor, center);
        }
    }

    private void TriangulateWaterShore(
        HexDirection direction,
        HexCellData cell,
        int cellIdx,
        int neighborIdx,
        int neighborColumnIdx,
        Vector3 center)
    {
        EdgeVertices e1 = new EdgeVertices(
            center + HexMetrics.GetFirstWaterCorner(direction),
            center + HexMetrics.GetSecondWaterCorner(direction)
        );

        water.AddTriangle(center, e1.v1, e1.v2);
        water.AddTriangle(center, e1.v2, e1.v3);
        water.AddTriangle(center, e1.v3, e1.v4);
        water.AddTriangle(center, e1.v4, e1.v5);
        Vector3 indices = new Vector3(cellIdx, neighborIdx, cellIdx);
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);

        Vector3 center2 = Grid.CellPositions[neighborIdx];
        int cellColumnIndex = cell.coordinates.ColumnIndex;
        if (neighborColumnIdx < cellColumnIndex - 1)
        {
            center2.x += HexMetrics.wrapSize * HexMetrics.innerDiameter;
        }
        else if (neighborColumnIdx > cellColumnIndex + 1)
        {
            center2.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter;
        }
        center2.y = center.y;
        EdgeVertices e2 = new EdgeVertices(
            center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
            center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite())
        );

        if (cell.HasRiverThroughEdge(direction))
        {
            TriangulateEstuary(e1, e2, cell.HasIncomingRiverThroughEdge(direction), indices);
        }
        else
        {
            waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
        }


        HexCoordinates nextNeighborCoordinates = cell.coordinates.Step(direction.Next());
        if (Grid.GetCellIdx(
            nextNeighborCoordinates, out int nextNeighborIndex))
        {
            Vector3 center3 = Grid.CellPositions[nextNeighborIndex];
            bool nextNeighborIsUnderwater = Grid.CellData[nextNeighborIndex].IsUnderwater;
            int nextNeighborColumnIndex = nextNeighborCoordinates.ColumnIndex;
            if (nextNeighborColumnIndex < cellColumnIndex - 1)
            {
                center3.x += HexMetrics.wrapSize * HexMetrics.innerDiameter;
            }
            else if (nextNeighborColumnIndex > cellColumnIndex + 1)
            {
                center3.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter;
            }
            HexDirection prev = direction.Previous();
            Vector3 v3 = center3 + (nextNeighborIsUnderwater ?
                                 HexMetrics.GetFirstWaterCorner(prev) :
                                 HexMetrics.GetFirstSolidCorner(prev)
                            );
            v3.y = center.y;
            waterShore.AddTriangle(e1.v5, e2.v5, v3);
            waterShore.AddTriangleUV(
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, nextNeighborIsUnderwater ? 0f : 1f)
            );
            indices.z = nextNeighborIndex;
            waterShore.AddTriangleCellData(indices, weights1, weights2, weights3);
        }
    }

    void TriangulateEstuary(
        EdgeVertices e1, EdgeVertices e2, bool incomingRiver, Vector3 indices
    )
    {
        waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
        waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
        waterShore.AddTriangleUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
        waterShore.AddTriangleUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
        waterShore.AddTriangleCellData(indices, weights2, weights1, weights1);
        waterShore.AddTriangleCellData(indices, weights2, weights1, weights1);

        estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
        estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
        estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

        estuaries.AddQuadUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 0f)
        );
        estuaries.AddTriangleUV(
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f)
        );
        estuaries.AddQuadUV(
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f)
        );
        estuaries.AddQuadCellData(indices, weights2, weights1, weights2, weights1);
        estuaries.AddTriangleCellData(indices, weights1, weights2, weights2);
        estuaries.AddQuadCellData(indices, weights1, weights2);

        //这段uv描述河口(溪流到大海)区域的扩散过程
        //具体我也不太懂，用到时回来要了解下
        if (incomingRiver)
        {
            estuaries.AddQuadUV1(
                new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f),
                new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f)
            );
            estuaries.AddTriangleUV1(
                new Vector2(0.5f, 1.1f),
                new Vector2(1f, 0.8f),
                new Vector2(0f, 0.8f)
            );
            estuaries.AddQuadUV1(
                new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f),
                new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f)
            );
        }
        else
        {
            estuaries.AddQuadUV1(
                new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
                new Vector2(0f, 0f), new Vector2(0.5f, -0.3f)
            );
            estuaries.AddTriangleUV1(
                new Vector2(0.5f, -0.3f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f)
            );
            estuaries.AddQuadUV1(
                new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
                new Vector2(1f, 0f), new Vector2(1.5f, -0.2f)
            );
        }

    }

    private void TriangulateOpenWater(
        HexCoordinates coordinates,
        HexDirection direction,
        int cellIdx,
        int neighborIdx,
        Vector3 center)
    {
        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);
        water.AddTriangle(center, c1, c2);

        Vector3 indices = new Vector3(cellIdx, cellIdx, cellIdx);
        water.AddTriangleCellData(indices, weights1);

        if (direction <= HexDirection.BottomRight && neighborIdx != -1)
        {
            Vector3 bridge = HexMetrics.GetWaterBridge(direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;
            indices.y = neighborIdx;
            water.AddQuad(c1, c2, e1, e2);
            water.AddQuadCellData(indices, weights1, weights2);

            if (direction <= HexDirection.Right)
            {
                //HexCell nextNeighbor = cell.GetNeighbor(Grid, direction.Next());
                if (!Grid.GetCellIdx(coordinates.Step(direction.Next()), out int nextNeighborIdx) ||
                    !Grid.CellData[nextNeighborIdx].IsUnderwater)
                {
                    return;
                }
                indices.z = nextNeighborIdx;
                water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));
                water.AddTriangleCellData(indices, weights1, weights2, weights3);
            }
        }

        return;
    }

    void TriangulateAdjacentToRiver(HexDirection direction,
        HexCellData cell,
        int cellIndex,
        Vector3 center,
        EdgeVertices e)
    {
        if (cell.HasRoads)
        {
            TriangulateRoadAdjacentToRiver(direction, cell, cellIndex, center, e);
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

        TriangulateEdgeStrip(m, weights1, cellIndex, e, weights1, cellIndex);
        TriangulateEdgeFan(center, m, cellIndex);

        if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
        {
            features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
        }
    }

    void TriangulateRoadAdjacentToRiver(HexDirection direction,
            HexCellData cell,
            int cellIdx,
            Vector3 center,
            EdgeVertices e)
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
            if (cell.IncomingRiver == direction.Next() && (
                cell.HasRoadThroughEdge(direction.Next2()) ||
                cell.HasRoadThroughEdge(direction.Opposite())
            ))
            {
                features.AddBridge(roadCenter, center - corner * 0.5f);
            }
            center += corner * 0.25f;
        }
        else if (HasZigzagRiverPrev(cell))
        {
            roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.25f;
        }
        else if (HasZigzagRiverNext(cell))
        {
            roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.25f;
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
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(middle);
            roadCenter += offset * 0.25f;
            if (direction == middle &&
                cell.HasRoadThroughEdge(direction.Opposite()))
            {
                features.AddBridge(
                    roadCenter,
                    center - offset * (HexMetrics.innerToOuter * 0.7f)
                );
            }

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
            TriangulateRoadEdge(roadCenter, center, mL, cellIdx);
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
            TriangulateRoadEdge(roadCenter, mR, center, cellIdx);
        }

        if (hasRoadThroughEdge)
        {
            TriangulateRoad(roadCenter, mL, mR, e, cellIdx);
        }
        else
        {
            TriangulateRoadEdge(roadCenter, mL, mR, cellIdx);
        }
    }

    private bool HasStraightRiver(HexCellData cell)
    {
        HexCellFlags flags = cell.flags;

        if (!flags.HasAny(HexCellFlags.RiverIn) ||
        !flags.HasAny(HexCellFlags.RiverOut))
        {
            return false;
        }

        return flags.RiverInDirection() == flags.RiverOutDirection().Opposite();
    }


    private bool HasZigzagRiverPrev(HexCellData cell)
    {
        HexCellFlags flags = cell.flags;

        if (!flags.HasAny(HexCellFlags.RiverIn) ||
        !flags.HasAny(HexCellFlags.RiverOut))
        {
            return false;
        }

        return flags.RiverInDirection() == flags.RiverOutDirection().Previous();
    }

    private bool HasZigzagRiverNext(HexCellData cell)
    {
        HexCellFlags flags = cell.flags;

        if (!flags.HasAny(HexCellFlags.RiverIn) ||
        !flags.HasAny(HexCellFlags.RiverOut))
        {
            return false;
        }

        return flags.RiverInDirection() == flags.RiverOutDirection().Next();
    }

    void TriangulateWithRiverBeginOrEnd(
        HexCellData cell,
        int cellIdx,
        Vector3 center,
        EdgeVertices e)
    {
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );
        m.v3.y = e.v3.y;

        TriangulateEdgeStrip(m, weights1, cellIdx, e, weights1, cellIdx);
        TriangulateEdgeFan(center, m, cellIdx);

        if (!cell.IsUnderwater)
        {
            bool reversed = cell.flags.HasAny(HexCellFlags.RiverIn);
            Vector3 indices = new Vector3(cellIdx, cellIdx, cellIdx);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed, indices);
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
            rivers.AddTriangleCellData(indices, weights1);
        }
    }

    void TriangulateWithRiver(
        HexDirection direction,
        HexCellData cell,
        int cellIdx,
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
        TriangulateEdgeStrip(m, weights1, cellIdx, e, weights1, cellIdx);
        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddTriangle(centerR, m.v4, m.v5);

        Vector3 indices;
        indices.x = indices.y = indices.z = cellIdx;
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddQuadCellData(indices, weights1);
        terrain.AddQuadCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);

        if (!cell.IsUnderwater)
        {
            bool reversed = cell.flags.HasRiverIn(direction);
            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed, indices);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed, indices);
        }
    }

    private void TriangulateConnection(
        HexDirection direction,
        HexCellData cell,
        int cellIdx,
        float y,
        EdgeVertices e1)
    {

        if (!Grid.GetCellIdx(cell.coordinates.Step(direction), out int neighborIdx))
        {
            return;
        }

        HexCellData neighborData = Grid.CellData[neighborIdx];
        Vector3 neighborPosition = Grid.CellPositions[neighborIdx];

        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighborPosition.y - y;
        EdgeVertices e2 = new EdgeVertices(e1.v1 + bridge, e1.v5 + bridge);

        if (cell.HasRiverThroughEdge(direction))
        {
            e2.v3.y = neighborData.StreamBedY;
            Vector3 indices = new Vector3(cellIdx, cellIdx, neighborIdx);
            if (!cell.IsUnderwater)
            {
                if (!neighborData.IsUnderwater)
                {
                    TriangulateRiverQuad(e1.v2, e1.v4, e2.v2, e2.v4,
                      cell.RiverSurfaceY, neighborData.RiverSurfaceY, 0.8f,
                      cell.HasIncomingRiverThroughEdge(direction),
                      indices
                    );
                }
                else if (cell.Elevation > neighborData.WaterLevel)
                {
                    TriangulateWaterfallInWater(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighborData.RiverSurfaceY, neighborData.WaterSurfaceY,
                        indices
                    );
                }
            }
            else if (!neighborData.IsUnderwater && neighborData.Elevation > cell.WaterLevel)
            {
                TriangulateWaterfallInWater(
                    e2.v4, e2.v2, e1.v4, e1.v2,
                    neighborData.RiverSurfaceY, cell.RiverSurfaceY, cell.WaterSurfaceY,
                    indices
                );
            }
        }


        bool hasRoad = cell.HasRoadThroughEdge(direction);
        if (cell.GetEdgeType(neighborData) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(e1, cellIdx, e2, neighborIdx, hasRoad);
        }
        else
        {
            TriangulateEdgeStrip(e1, weights1, cellIdx, e2, weights2, neighborIdx, hasRoad);
        }

        features.AddWall(e1, cell, e2, neighborData, direction);


        if (direction <= HexDirection.Right && Grid.GetCellIdx(cell.coordinates.Step(direction.Next()), out int nextNeighborIdx))
        {
            HexCellData nextNeighborData = Grid.CellData[nextNeighborIdx];
            Vector3 nextNeighborPosition = Grid.CellPositions[nextNeighborIdx];
            Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighborPosition.y;

            //work from bottom to left then to right
            //left     right
            //   bottom
            if (cell.Elevation <= neighborData.Elevation)
            {
                if (cell.Elevation <= nextNeighborData.Elevation)
                {
                    TriangulateCorner(e1.v5, cellIdx, e2.v5, neighborIdx, v5, nextNeighborIdx);
                }
                else
                {
                    TriangulateCorner(v5, nextNeighborIdx, e1.v5, cellIdx, e2.v5, neighborIdx);
                }
            }
            else if (neighborData.Elevation <= nextNeighborData.Elevation)
            {
                TriangulateCorner(e2.v5, neighborIdx, v5, nextNeighborIdx, e1.v5, cellIdx);
            }
            else
            {
                TriangulateCorner(v5, nextNeighborIdx, e1.v5, cellIdx, e2.v5, neighborIdx);
            }
        }
    }

    void TriangulateWaterfallInWater(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float waterY, Vector3 indices)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);
        float t = (waterY - y2) / (y1 - y2);
        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);
        rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
        rivers.AddQuadCellData(indices, weights1, weights2);
    }

    private void TriangulateEdgeTerraces(
        EdgeVertices begin, int beginCellIdx,
        EdgeVertices end, int endCellIdx, bool hasRoad)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color w2 = HexMetrics.TerraceLerpColor(weights1, weights2, 1);
        float i1 = beginCellIdx;
        float i2 = endCellIdx;
        TriangulateEdgeStrip(begin, weights1, i1, e2, w2, i2, hasRoad);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color w1 = w2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            w2 = HexMetrics.TerraceLerpColor(weights1, weights2, i);
            TriangulateEdgeStrip(e1, w1, i1, e2, w2, i2, hasRoad);
        }

        TriangulateEdgeStrip(e2, w2, i1, end, weights2, i2, hasRoad);
    }

    void TriangulateCorner(
        Vector3 bottom, int bottomCellIdx,
        Vector3 left, int leftCellIdx,
        Vector3 right, int rightCellIdx
    )
    {
        HexEdgeType leftEdgeType = Grid.CellData[bottomCellIdx].GetEdgeType(Grid.CellData[leftCellIdx]);
        HexEdgeType rightEdgeType = Grid.CellData[bottomCellIdx].GetEdgeType(Grid.CellData[rightCellIdx]);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            switch (rightEdgeType)
            {
                case HexEdgeType.Slope:
                    {
                        TriangulateCornerTerraces(
                            bottom, bottomCellIdx,
                            left, leftCellIdx,
                            right, rightCellIdx
                        );
                        break;
                    }
                case HexEdgeType.Flat:
                    {
                        TriangulateCornerTerraces(
                            left, leftCellIdx,
                            right, rightCellIdx,
                            bottom, bottomCellIdx
                        );
                        break;
                    }
                case HexEdgeType.Cliff:
                    {
                        TriangulateCornerTerracesCliff(
                            bottom, bottomCellIdx,
                            left, leftCellIdx,
                            right, rightCellIdx
                        );
                        break;
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
                   right, rightCellIdx,
                   bottom, bottomCellIdx,
                   left, leftCellIdx
               );
            }
            else if (leftEdgeType == HexEdgeType.Cliff)
            {
                TriangulateCornerCliffTerraces(
                    bottom, bottomCellIdx,
                    left, leftCellIdx,
                    right, rightCellIdx
                );
            }
        }
        else if (Grid.CellData[leftCellIdx].GetEdgeType(Grid.CellData[rightCellIdx]) == HexEdgeType.Slope)
        {
            if (Grid.CellData[leftCellIdx].Elevation < Grid.CellData[rightCellIdx].Elevation)
            {
                TriangulateCornerCliffTerraces(right, rightCellIdx, bottom, bottomCellIdx, left, leftCellIdx);
            }
            else
            {
                TriangulateCornerTerracesCliff(left, leftCellIdx, right, rightCellIdx, bottom, bottomCellIdx);
            }

        }
        else
        {
            terrain.AddTriangle(bottom, left, right);
            terrain.AddTriangleCellData(
                new Vector3(
                    bottomCellIdx,
                    leftCellIdx,
                    rightCellIdx
                ),
                weights1,
                weights2,
                weights3
            );
        }

        features.AddWall(
            bottom, Grid.CellData[bottomCellIdx],
            left, Grid.CellData[leftCellIdx],
            right, Grid.CellData[rightCellIdx]
        );
    }

    void TriangulateCornerTerraces(
        Vector3 begin, int beginCellIdx,
        Vector3 left, int leftCellIdx,
        Vector3 right, int rightCellIdx
    )
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color w3 = HexMetrics.TerraceLerpColor(weights1, weights2, 1);
        Color w4 = HexMetrics.TerraceLerpColor(weights1, weights3, 1);
        Vector3 indices = new Vector3(beginCellIdx, leftCellIdx, rightCellIdx);

        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleCellData(indices, weights1, w3, w4);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color w1 = w3;
            Color w2 = w4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            w3 = HexMetrics.TerraceLerpColor(weights1, weights2, i);
            w4 = HexMetrics.TerraceLerpColor(weights1, weights3, i);
            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadCellData(indices, w1, w2, w3, w4);
        }

        terrain.AddQuad(v3, v4, left, right);
        terrain.AddQuadCellData(indices, w3, w4, weights2, weights3);
    }

    void TriangulateCornerTerracesCliff(
        Vector3 begin, int beginCellIdx,
        Vector3 left, int leftCellIdx,
        Vector3 right, int rightCellIdx
    )
    {
        HexCellData beginCell = Grid.CellData[beginCellIdx];
        HexCellData rightCell = Grid.CellData[rightCellIdx];
        HexCellData leftCell = Grid.CellData[leftCellIdx];
        //数学真神奇
        float b = Mathf.Abs(1f / (rightCell.Elevation - beginCell.Elevation));
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        Color boundaryWeights = Color.Lerp(weights1, weights3, b);
        Vector3 indices = new Vector3(beginCellIdx, leftCellIdx, rightCellIdx);

        TriangulateBoundaryTriangle(begin, weights1, left, weights2, boundary, boundaryWeights, indices);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, weights2, right, weights3, boundary, boundaryWeights, indices);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleCellData(
                indices, weights2, weights3, boundaryWeights
            );
        }
    }

    void TriangulateCornerCliffTerraces(
        Vector3 begin, int beginCellIdx,
        Vector3 left, int leftCellIdx,
        Vector3 right, int rightCellIdx
    )
    {
        HexCellData beginCell = Grid.CellData[beginCellIdx];
        HexCellData rightCell = Grid.CellData[rightCellIdx];
        HexCellData leftCell = Grid.CellData[leftCellIdx];
        float b = Mathf.Abs(1f / (leftCell.Elevation - beginCell.Elevation));

        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        Color boundaryWeights = Color.Lerp(weights1, weights2, b);
        Vector3 indices = new Vector3(beginCellIdx, leftCellIdx, rightCellIdx);

        TriangulateBoundaryTriangle(right, weights3, begin, weights1, boundary, boundaryWeights, indices);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, weights2, right, weights3, boundary, boundaryWeights, indices);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleCellData(indices, weights2, weights3, boundaryWeights);
        }
    }

    private void TriangulateBoundaryTriangle(
        Vector3 begin, Color beginWeights,
        Vector3 left, Color leftWeights,
        Vector3 boundary, Color boundaryWeights,
        Vector3 indices
    )
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color w2 = HexMetrics.TerraceLerpColor(beginWeights, leftWeights, 1);
        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        terrain.AddTriangleCellData(indices, beginWeights, w2, boundaryWeights);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color w1 = w2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            w2 = HexMetrics.TerraceLerpColor(beginWeights, leftWeights, i);
            terrain.AddTriangleUnperturbed(v1, v2, boundary);
            terrain.AddTriangleCellData(indices, w1, w2, boundaryWeights);
        }

        terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleCellData(indices, w2, leftWeights, boundaryWeights);
    }

    void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y, float v, bool reversed, Vector3 indices
        )
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed, indices);
    }

    void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float v, bool reversed, Vector3 indices
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
        rivers.AddQuadCellData(indices, weights1, weights2);
    }

    void TriangulateWithoutRiver(
        HexDirection direction,
        HexCellData cell,
        int cellIndex,
        Vector3 center,
        EdgeVertices e)
    {
        TriangulateEdgeFan(center, e, cellIndex);
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
                    e,
                    cellIndex
                );
            }
            else
            {
                TriangulateRoadEdge(center, mL, mR, cellIndex);
            }
        }
    }

    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float index)
    {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangle(center, edge.v4, edge.v5);

        Vector3 indices = new Vector3(index, index, index);
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
    }

    void TriangulateEdgeStrip(
        EdgeVertices e1, Color w1, float index1,
        EdgeVertices e2, Color w2, float index2,
        bool hasRoad = false)
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

        Vector3 indices;
        indices.x = indices.z = index1;
        indices.y = index2;
        terrain.AddQuadCellData(indices, w1, w2);
        terrain.AddQuadCellData(indices, w1, w2);
        terrain.AddQuadCellData(indices, w1, w2);
        terrain.AddQuadCellData(indices, w1, w2);

        if (hasRoad)
        {
            TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4,
            w1, w2, indices);
        }
    }

    void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e, float index)
    {
        Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
        TriangulateRoadSegment(
            mL, mC, mR, e.v2, e.v3, e.v4,
            weights1, weights2, new Vector3(index, index, index)
        );
        roads.AddTriangle(center, mL, mC);
        roads.AddTriangle(center, mC, mR);
        roads.AddTriangleUV(
            new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f)
        );
        roads.AddTriangleUV(
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f)
        );
        Vector3 indices = new Vector3(index, index, index);
        roads.AddTriangleCellData(indices, weights1);
        roads.AddTriangleCellData(indices, weights1);
    }

    void TriangulateRoadSegment(
        Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 v4, Vector3 v5, Vector3 v6,
        Color w1, Color w2, Vector3 indices
    )
    {
        roads.AddQuad(v1, v2, v4, v5);
        roads.AddQuad(v2, v3, v5, v6);
        roads.AddQuadUV(0f, 1f, 0f, 0f);
        roads.AddQuadUV(1f, 0f, 0f, 0f);
        roads.AddQuadCellData(indices, w1, w2);
        roads.AddQuadCellData(indices, w1, w2);
    }

    void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR, float index)
    {
        roads.AddTriangle(center, mL, mR);
        roads.AddTriangleUV(
            new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
        Vector3 indices = new Vector3(index, index, index);
        roads.AddTriangleCellData(indices, weights1);
    }

    Vector2 GetRoadInterpolators(HexDirection direction, HexCellData cell)
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
            estuaries.DrawGizmos();
        }
        if (gizmoMode.HasFlag(GizmoMode.Road))
        {
            Gizmos.color = Color.green;
            roads.DrawGizmos();
        }

        features.DrawGizmos(gizmoMode);
    }

}