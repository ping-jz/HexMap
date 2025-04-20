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
    public TextMeshPro cellLabelPrefab;
    Canvas gridCanvas;
    HexCell[] cells;
    HexMesh hexMesh;
    MeshCollider meshCollider;

    void Awake()
    {
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
        position.x = (x + (z & 1) * 0.5f ) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate(cellPrefab);
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        TextMeshPro label = Instantiate(cellLabelPrefab);
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.SetText(cell.coordinates.ToStringOnSeparateLines());
    }

    void Start()
    {
        hexMesh.Triangulate(cells);
    }

    void Update()
    {
        if(Input.GetMouseButton(0)) {
            HandleInput();
        }
    }

    void HandleInput() {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(inputRay, out hit)) {
            TouchCell(hit.point);
        }
    }

    void TouchCell(Vector3 position) {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        Debug.Log("Touched at:" + coordinates);
        Debug.Log("Touched at pos:" + position);
    }

}