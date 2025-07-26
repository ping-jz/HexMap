using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class HexGameUI : MonoBehaviour
{
    [SerializeField]
    private UIDocument sidePanels;
    [SerializeField]
    HexGrid grid;
    int currentCellIndex;
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


        if (currentCellIndex >= 0 &&
            selectedUnit.IsValidDestination(grid.CellData[currentCellIndex]))
        {
            grid.FindPath(selectedUnit.Location, currentCellIndex, selectedUnit);
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
        int index = cell ? cell.Index : -1;
        if (index != currentCellIndex)
        {
            currentCellIndex = index;
            return true;
        }
        return false;
    }

    void DoSelection()
    {
        grid.ClearPath();
        UpdateCurrentCell();
        if (currentCellIndex >= 0)
        {
            selectedUnit = grid.GetCell(currentCellIndex).Unit;
        }
    }

    public void SetEditMode(bool toggle)
    {
        StopAllCoroutines();
        enabled = !toggle;
        StartCoroutine(grid.ShowUI(!toggle));
        grid.ClearPath();
        selectedUnit = null;
        currentCellIndex = -1;
        if (toggle)
        {
            Shader.EnableKeyword("_HEX_MAP_EDIT_MODE");
        }
        else
        {
            Shader.DisableKeyword("_HEX_MAP_EDIT_MODE");
        }
    }

    void DoMove()
    {
        if (grid.HasPath)
        {
            selectedUnit.Travel(grid.GetPath());
            grid.ClearPath();
        }
    }

}