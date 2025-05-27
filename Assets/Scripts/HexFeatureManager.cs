using System.Collections.Generic;
using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    [SerializeField]
    private HexFeatureCollection[] urbanPrefabs, farmPrefabs, plantPrefabs;
    [SerializeField]
    private HexMesh walls;

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
        //why multi 0.25.increase the probability ？
        if (hash.a >= cell.UrbanLevel * 0.25f)
        {
            return;
        }

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

    public void AddWall(EdgeVertices near, HexCell nearCell,
    EdgeVertices far, HexCell farCell)
    {
        if (nearCell.Walled == farCell.Walled)
        {
            return;
        }

        AddWallSegment(near.v1, far.v1, near.v2, far.v2);
        AddWallSegment(near.v2, far.v2, near.v3, far.v3);
        AddWallSegment(near.v3, far.v3, near.v4, far.v4);
        AddWallSegment(near.v4, far.v4, near.v5, far.v5);
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
        AddWallSegment(pivot, left, pivot, right);
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