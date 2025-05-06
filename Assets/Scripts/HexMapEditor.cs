using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

enum OptionalToggle
{
    Ignore, Yes, No
}



[Flags]
enum EditorFlags
{
    Nothing = 0,
    ApplyElevation = 0b0001,
    ApplyColor = 0b0010,
    Drag = 0b0100
}

static class EditorFlagsExtensions
{
    public static bool Has(this EditorFlags flags, EditorFlags mask) =>
        (flags & mask) == mask;

    public static bool HasAny(this EditorFlags flags, EditorFlags mask) =>
        (flags & mask) != 0;
    public static bool HasNot(this EditorFlags flags, EditorFlags mask) =>
        (flags & mask) != mask;

    public static EditorFlags With(this EditorFlags flags, EditorFlags mask) =>
        flags | mask;
    public static EditorFlags Without(this EditorFlags flags, EditorFlags mask) =>
        flags & ~mask;
}


public class HexMapEditor : MonoBehaviour
{
    [SerializeField]
    private Color[] colors;
    [SerializeField]
    private HexGrid hexGrid;
    [SerializeField]
    private UIDocument sidePanels;
    private Color activeColor;
    int activeElevation;
    EditorFlags flags = EditorFlags.ApplyColor.With(EditorFlags.ApplyElevation);
    int brushSize;
    OptionalToggle riverMode;
    HexDirection dragDirection;
    HexCell previousCell;

    void Awake()
    {
        SelectColor(1);

        RegisterEvents();
    }

    private void RegisterEvents()
    {
        VisualElement root = sidePanels.rootVisualElement;
        if (root == null)
        {
            return;
        }

        root.Q<RadioButtonGroup>("Colors").RegisterValueChangedCallback(change => SelectColor(change.newValue));
        root.Q<Toggle>("ApplyElevation").RegisterValueChangedCallback(change =>
        {
            if (change.newValue)
            {
                flags = flags.With(EditorFlags.ApplyElevation);
            }
            else
            {
                flags = flags.Without(EditorFlags.ApplyElevation);
            }
        });
        root.Q<SliderInt>("Elevation").RegisterValueChangedCallback(change => SetElevation(change.newValue));
        root.Q<SliderInt>("BrushSize").RegisterValueChangedCallback(change => SetBrushSize(change.newValue));
        root.Q<Toggle>("ShowUI").RegisterValueChangedCallback(change => hexGrid.ShowUI(change.newValue));
        root.Q<RadioButtonGroup>("River").RegisterValueChangedCallback(change => SetRiverMode(change.newValue));
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
        else
        {
            previousCell = null;
        }
    }


    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            HexCell cell = hexGrid.GetCell(hit.point);
            if (previousCell && previousCell != cell)
            {
                ValidateDrag(cell);
            }
            else
            {
                flags = flags.Without(EditorFlags.Drag);
            }
            EditCells(cell);
            previousCell = cell;
        }
        else
        {
            previousCell = null;
        }
    }

    void ValidateDrag(HexCell cell)
    {
        for (
            dragDirection = HexDirection.NE;
            dragDirection <= HexDirection.NW;
            dragDirection++
        )
        {
            if (previousCell.GetNeighbor(dragDirection) == cell)
            {
                flags = flags.With(EditorFlags.Drag);
                return;
            }
        }

        flags = flags.Without(EditorFlags.Drag);
    }

    private void EditCells(HexCell center)
    {
        int centerX = center.Coordinates.X;
        int centerZ = center.Coordinates.Z;

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

        if (flags.Has(EditorFlags.ApplyColor))
        {
            cell.color = activeColor;
        }

        if (flags.Has(EditorFlags.ApplyElevation))
        {
            cell.Elevation = activeElevation;
        }

        if (riverMode == OptionalToggle.No)
        {
            refrechCells(cell.RemoveRivers());
        }
        else if (flags.Has(EditorFlags.Drag) && riverMode == OptionalToggle.Yes)
        {
            HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
            if (otherCell)
            {
                refrechCells(otherCell.SetOutgoingRiver(dragDirection));
            }
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

    void refrechCells(IEnumerable<HexCell> cells)
    {
        foreach (HexCell cell in cells)
        {
            hexGrid.GetChunk(cell).Refresh();
        }
    }

    public void SelectColor(int index)
    {
        flags = index > 0 ? flags.With(EditorFlags.ApplyColor) : flags.Without(EditorFlags.ApplyColor);

        if (flags.Has(EditorFlags.ApplyColor))
        {
            activeColor = colors[index - 1];
        }
    }

    public void SetApplyElevation(bool toggle)
    {
        flags = toggle ? flags.With(EditorFlags.ApplyElevation) : flags.Without(EditorFlags.ApplyElevation);
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }

    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }

    void OnValidate()
    {
        RegisterEvents();
    }
}