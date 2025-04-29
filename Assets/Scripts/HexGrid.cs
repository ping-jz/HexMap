using TMPro;
using UnityEngine;

public class HexGrid : MonoBehaviour
{

    [SerializeField]
    private HexCell cellPrefab;
    [SerializeField]
    private HexGridChunk chunkPrefab;
    [SerializeField]
    private TextMeshPro cellLabelPrefab;
    [SerializeField]
    private Color defaultColor = Color.white;
    [SerializeField]
    private Texture2D noiseSource;
    [SerializeField]
    private int chunkCountX = 4, chunkCountZ = 3;
    private int cellCountX = 6, cellCountZ = 6;
    HexCell[] cells;
    HexGridChunk[] chunks;

    public int ChunkCountX
    {
        get { return chunkCountX; }
    }

    public int ChunkCountZ
    {
        get { return chunkCountZ; }
    }

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;

        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCell();
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int i = 0; i < chunks.Length; i++)
        {
            HexGridChunk chunk = chunks[i] = Instantiate(chunkPrefab);
            chunk.transform.SetParent(transform, false);
        }
    }

    private void CreateCell()
    {
        cells = new HexCell[cellCountZ * cellCountX];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
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
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.color = defaultColor;

        TextMeshPro label = Instantiate(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.SetText(cell.coordinates.ToStringOnSeparateLines());
        cell.uiRect = label.rectTransform;
        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                //基数行最后一个网格没有SE方向的邻居
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }
    }

    void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        int chunkIdx = chunkX + chunkZ * chunkCountX;
        HexGridChunk chunk = chunks[chunkIdx];

        //这个本地坐标转换不错的，计算x,z经过了多少个网格块
        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        cell.ChunkIdx = chunkIdx;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    void OnEnable()
    {
        HexMetrics.noiseSource = noiseSource;
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return cells[index];
    }

    public HexGridChunk GetChunk(HexCell cell)
    {
        return chunks[cell.ChunkIdx];
    }
}