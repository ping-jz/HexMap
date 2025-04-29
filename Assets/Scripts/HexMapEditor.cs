using System.IO.Compression;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    [SerializeField]
    private Color[] colors;
    [SerializeField]
    private HexGrid hexGrid;
    private Color activeColor;
    int activeElevation;
    bool applyElevation = true;
    bool applyColor = true;
    int brushSize;

    void Awake()
    {
        SelectColor(0);
    }

    void Update()
    {
        if (
            Input.GetMouseButton(0) &&
            !EventSystem.current.IsPointerOverGameObject()
        )
        {
            HandleInput();
        }
    }


    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            HexCell cell = hexGrid.GetCell(hit.point);
            EditCells(cell);
        }
    }

    private void EditCells(HexCell center)
    {
        int centerX = center.Coordinates.X;
        int centerZ = center.Coordinates.Z;
        Debug.Log(center.Coordinates);

        //从中间往下走
        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
        {
            for (int x = centerX - r; x <= centerX + brushSize; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

        //从顶部往下走
        for (int r = 0, z = centerZ + brushSize; centerZ < z; z--, r++)
        {
            for (int x = centerX - brushSize; x <= centerX + r; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    private void EditCell(HexCell cell)
    {
        if (cell == null)
        {
            return;
        }
        if (applyColor)
        {
            cell.color = activeColor;
        }
        if (applyElevation)
        {
            cell.Elevation = activeElevation;
        }
        HexGridChunk chunk = hexGrid.GetChunk(cell);
        if (chunk)
        {
            chunk.Refresh();
            foreach (HexCell neighbor in cell.Neighbors)
            {
                if (
                    neighbor != null &&
                    (chunk = hexGrid.GetChunk(neighbor)) != null)
                {
                    chunk.Refresh();
                }
            }
        }
    }

    public void SelectColor(int index)
    {
        applyColor = index >= 0;
        if (applyColor)
        {
            activeColor = colors[index];
        }
    }

    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
        Debug.Log(brushSize);
    }
}