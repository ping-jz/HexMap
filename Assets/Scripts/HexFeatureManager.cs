using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    [SerializeField]
    private Transform featurePrefab;

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
        Transform instance = Instantiate(featurePrefab);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.a, 0f);
        instance.SetParent(container, false);   
    }
}