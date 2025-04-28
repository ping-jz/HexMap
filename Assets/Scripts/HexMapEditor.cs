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
            EditCell(cell);
        }
    }

    private void EditCell(HexCell cell)
    {
        cell.color = activeColor;
        cell.Elevation = activeElevation;
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
        activeColor = colors[index];
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }
}