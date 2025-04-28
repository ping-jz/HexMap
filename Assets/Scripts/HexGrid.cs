using TMPro;
using UnityEngine;

public class HexGrid : MonoBehaviour
{

    [SerializeField]
    private int width = 6;
    [SerializeField]
    private int height = 6;
    [SerializeField]
    private HexCell cellPrefab;
    [SerializeField]
    private TextMeshPro cellLabelPrefab;
    [SerializeField]
    private Color defaultColor = Color.white;
    [SerializeField]
    private Texture2D noiseSource;
    Canvas gridCanvas;
    HexCell[] cells;
    HexMesh hexMesh;
    MeshCollider meshCollider;

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();

        cells = new HexCell[height * width];

        for (int z = 0, i = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate(cellPrefab);
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.color = defaultColor;

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - width]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - width]);
                //基数行最后一个网格没有SE方向的邻居
                if (x < width - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
                }
            }
        }

        TextMeshPro label = Instantiate(cellLabelPrefab);
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.SetText(cell.coordinates.ToStringOnSeparateLines());

        cell.uiRect = label.rectTransform;
        cell.Elevation = 0;
    }

    void Start()
    {
        hexMesh.Triangulate(cells);
    }

    void OnEnable()
    {
        HexMetrics.noiseSource = noiseSource;
    }


    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
        return cells[index];
    }

    public void Refresh()
    {
        hexMesh.Triangulate(cells);
    }

}