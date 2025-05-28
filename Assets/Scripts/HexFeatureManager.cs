using System.Collections.Generic;
using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    [SerializeField]
    private HexFeatureCollection[] urbanPrefabs, farmPrefabs, plantPrefabs;
    [SerializeField]
    private HexMesh walls;
    [SerializeField]
    private Transform wallTower;

    private Transform container;


    public void Clear()
    {
        if (container)
        {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
        walls.Clear();
    }

    public void Apply()
    {
        walls.Apply();
    }

    public void AddFeature(HexCell cell, Vector3 position)
    {
        HexHash hash = HexMetrics.SampleHashGrid(position);

        Transform urban = PickPrefab(urbanPrefabs, cell.UrbanLevel, hash.a, hash.d);
        Transform farm = PickPrefab(farmPrefabs, cell.FarmLevel, hash.b, hash.d);
        Transform plant = PickPrefab(plantPrefabs, cell.PlantLevel, hash.c, hash.d);

        List<Transform> transforms = ListPool<Transform>.Get();
        if (urban)
        {
            transforms.Add(urban);
        }
        if (farm)
        {
            transforms.Add(farm);
        }
        if (plant)
        {
            transforms.Add(plant);
        }
        if (transforms.Count == 0)
        {
            return;
        }
        Transform prefab = transforms[(int)(hash.e * transforms.Count)];
        if (!prefab)
        {
            return;
        }

        Transform instance = Instantiate(prefab);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.f, 0f);
        instance.SetParent(container, false);
    }

    Transform PickPrefab(HexFeatureCollection[] features, int level, float hash, float choice)
    {
        if (level <= 0)
        {
            return null;
        }

        float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
        for (int i = 0; i < thresholds.Length; i++)
        {
            if (hash < thresholds[i])
            {
                HexFeatureCollection prefabs = features[i];
                return prefabs.Pick(choice);
            }
        }

        return null;
    }

    public void AddWall(
        EdgeVertices near, HexCell nearCell,
        EdgeVertices far, HexCell farCell,
        HexDirection direction
    )
    {
        if (
            nearCell.Walled == farCell.Walled ||
            nearCell.IsUnderwater ||
            farCell.IsUnderwater ||
            nearCell.GetEdgeType(farCell) == HexEdgeType.Cliff
        )
        {
            return;
        }

        AddWallSegment(near.v1, far.v1, near.v2, far.v2);
        switch (nearCell.HasRiverThroughEdge(direction),
                nearCell.HasRoadThroughEdge(direction))
        {
            case (false, false):
                AddWallSegment(near.v2, far.v2, near.v3, far.v3);
                AddWallSegment(near.v3, far.v3, near.v4, far.v4);
                break;
            default:
                AddWallCap(near.v2, far.v2);
                //为什么要换位置为什么上面不用，基于什么原理
                //难道跟wallSegment一面，只能从一个方面看。所以才反过来吗？
                AddWallCap(far.v4, near.v4);
                break;
        }

        AddWallSegment(near.v4, far.v4, near.v5, far.v5);
    }

    void AddWallCap(Vector3 near, Vector3 far)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);
        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;
        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = center.y + HexMetrics.wallHeight;
        walls.AddQuadUnperturbed(v1, v2, v3, v4);
    }

    void AddWallSegment(
        Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight
    )
    {
        nearLeft = HexMetrics.Perturb(nearLeft);
        nearRight = HexMetrics.Perturb(nearRight);
        farLeft = HexMetrics.Perturb(farLeft);
        farRight = HexMetrics.Perturb(farRight);

        Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
        Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

        Vector3 leftThicknessOffset =
            HexMetrics.WallThicknessOffset(nearLeft, farLeft);
        Vector3 rightThicknessOffset =
            HexMetrics.WallThicknessOffset(nearRight, farRight);

        float leftTop = left.y + HexMetrics.wallHeight;
        float rightTop = right.y + HexMetrics.wallHeight;

        Vector3 v1, v2, v3, v4;
        v1 = v3 = left - leftThicknessOffset;
        v2 = v4 = right - rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        walls.AddQuadUnperturbed(v1, v2, v3, v4);

        Vector3 nearTopLeft = v3, farTopLeft = v4;

        v1 = v3 = left + leftThicknessOffset;
        v2 = v4 = right + rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        walls.AddQuadUnperturbed(v2, v1, v4, v3);

        Vector3 nearTopRight = v3, farTopRight = v4;

        walls.AddQuadUnperturbed(nearTopLeft, farTopLeft, nearTopRight, farTopRight);
    }

    void AddWallTower(
        Vector3 pivot, Vector3 farLeft, Vector3 farRight
    )
    {
        HexHash hash = HexMetrics.SampleHashGrid(
                (pivot + farLeft + farRight) * (1f / 3f)
        );

        if (hash.e > HexMetrics.wallTowerThreshold)
        {
            return;
        }

        pivot = HexMetrics.Perturb(pivot);
        farLeft = HexMetrics.Perturb(farLeft);
        farRight = HexMetrics.Perturb(farRight);

        Vector3 left = HexMetrics.WallLerp(pivot, farLeft);
        Vector3 right = HexMetrics.WallLerp(pivot, farRight);

        Transform towerInstance = Instantiate(wallTower);
        towerInstance.transform.localPosition = (left + right) * 0.5f;
        Vector3 rightDirection = right - left;
        rightDirection.y = 0f;
        towerInstance.transform.right = rightDirection;
        towerInstance.SetParent(container, false);
    }


    public void AddWall(
        Vector3 c1, HexCell cell1,
        Vector3 c2, HexCell cell2,
        Vector3 c3, HexCell cell3
    )
    {
        switch (cell1.Walled, cell2.Walled, cell3.Walled)
        {
            case (true, true, false): AddWallSegment(c3, cell3, c1, cell1, c2, cell2); break;
            case (false, false, true): AddWallSegment(c3, cell3, c1, cell1, c2, cell2); break;
            case (true, false, true): AddWallSegment(c2, cell2, c3, cell3, c1, cell1); break;
            case (false, true, false): AddWallSegment(c2, cell2, c3, cell3, c1, cell1); break;
            case (true, false, false): AddWallSegment(c1, cell1, c2, cell2, c3, cell3); break;
            case (false, true, true): AddWallSegment(c1, cell1, c2, cell2, c3, cell3); break;
        }
    }

    void AddWallSegment(
        Vector3 pivot, HexCell pivotCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        if (pivotCell.IsUnderwater)
        {
            return;
        }
        bool hasLeftCell = !leftCell.IsUnderwater && pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
        bool hasRightCell = !rightCell.IsUnderwater && pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

        switch (hasLeftCell, hasRightCell)
        {

            case (true, true):
                AddWallSegment(pivot, left, pivot, right);
                AddWallTower(pivot, left, right);
                break;
            //为什么两个AddWallWedge参数顺序不同？
            //你能解答吗？
            case (true, false):
                if (leftCell.Elevation < rightCell.Elevation)
                {
                    AddWallWedge(pivot, left, right);
                }
                else
                {
                    AddWallCap(pivot, left);
                }
                break;
            case (false, true):
                if (rightCell.Elevation < leftCell.Elevation)
                {
                    AddWallWedge(right, pivot, left);
                }
                else
                {
                    AddWallCap(right, pivot);
                }

                break;
        }
    }

    void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);
        point = HexMetrics.Perturb(point);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;
        Vector3 pointTop = point;
        point.y = center.y;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = pointTop.y = center.y + HexMetrics.wallHeight;
        walls.AddQuadUnperturbed(v1, point, v3, pointTop);
        walls.AddQuadUnperturbed(point, v2, pointTop, v4);
        walls.AddTriangleUnperturbed(pointTop, v3, v4);
    }

    public void DrawGizmos(GizmoMode gizmoMode)
    {
        if (gizmoMode.HasFlag(GizmoMode.Wall))
        {
            Gizmos.color = Color.black;
            walls.DrawGizmos();
        }
    }
}