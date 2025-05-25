using System.Collections.Generic;
using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    [SerializeField]
    private HexFeatureCollection[] urbanPrefabs, farmPrefabs, plantPrefabs;

    private Transform container;


    public void Clear()
    {
        if (container)
        {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
    }

    public void Apply()
    {

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
}