using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class HexGameUI : MonoBehaviour
{
    [SerializeField]
    private UIDocument sidePanels;
    public HexGrid grid;
    HexCell currentCell;
    HexUnit selectedUnit;

    void Awake()
    {
        RegisterEvents();
    }

    void Enable()
    {
        RegisterEvents();
    }

    void OnValidate()
    {
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        VisualElement root = sidePanels.rootVisualElement;
        if (root == null)
        {
            return;
        }

        root.Q<Toggle>("EditerMode").RegisterValueChangedCallback(change => SetEditMode(change.newValue));
    }

    void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButtonDown(0))
            {
                DoSelection();
            }
            else if (selectedUnit)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    DoMove();
                }
                else
                {
                    DoPathfinding();
                }
            }
        }
    }

    void DoPathfinding()
    {
        if (!UpdateCurrentCell())
        {
            return;
        }


        if (currentCell && IsValidDestination(currentCell))
        {
            grid.FindPath(selectedUnit.Location, currentCell, 24);
        }
        else
        {
            grid.ClearPath();
        }
    }

    bool UpdateCurrentCell()
    {
        HexCell cell =
            grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (cell != currentCell)
        {
            currentCell = cell;
            return true;
        }
        return false;
    }

    void DoSelection()
    {
        UpdateCurrentCell();
        if (currentCell)
        {
            selectedUnit = currentCell.Unit;
        }
    }

    public void SetEditMode(bool toggle)
    {
        enabled = !toggle;
        grid.ShowUI(!toggle);
        grid.ClearPath();
    }

    void DoMove()
    {
        if (grid.HasPath)
        {
            selectedUnit.Location = currentCell;
            grid.ClearPath();
        }
    }

    public bool IsValidDestination(HexCell cell)
    {
        return !cell.IsUnderwater && !cell.Unit;
    }
}