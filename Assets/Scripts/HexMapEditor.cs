using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

[Flags]
enum EditorFlags
{
    Nothing = 0,
    ApplyElevation = 0b0001,
    ApplyColor = 0b0010,
    Drag = 0b0100,
    RiverIgnore = 0b001_000,
    RiverYes = 0b010_000,
    RiverNo = 0b100_000,
    RiverOpts = 0b111_000,

    RoadIgnore = 0b001_000_000,
    RoadYes = 0b010_000_000,
    RoadNo = 0b100_000_000,
    RoadOpts = 0b111_000_000,
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
        root.Q<RadioButtonGroup>("Colors").value = Array.IndexOf(colors, activeColor) + 1;

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
        root.Q<Toggle>("ApplyElevation").value = flags.Has(EditorFlags.ApplyElevation);

        root.Q<SliderInt>("Elevation").RegisterValueChangedCallback(change => SetElevation(change.newValue));
        root.Q<SliderInt>("Elevation").value = activeElevation;

        root.Q<SliderInt>("BrushSize").RegisterValueChangedCallback(change => SetBrushSize(change.newValue));
        root.Q<SliderInt>("BrushSize").value = brushSize;

        root.Q<Toggle>("ShowUI").RegisterValueChangedCallback(change => hexGrid.ShowUI(change.newValue));

        root.Q<RadioButtonGroup>("River").RegisterValueChangedCallback(change => SetRiverMode(change.newValue));
        SetRiverMode(0);
        
        root.Q<RadioButtonGroup>("Road").RegisterValueChangedCallback(change => SetRoadMode(change.newValue));
        SetRoadMode(0);
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
            dragDirection = HexDirection.TopRight;
            dragDirection <= HexDirection.TopLeft;
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
            refrechCells(cell.RemoveInvalidRiver());
            for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
            {
                if (cell.HasRoadThroughEdge(d) && cell.GetElevationDifference(d) > 1)
                {
                    refrechCells(cell.RemoveRoad(d));
                }
            }
        }

        if (flags.Has(EditorFlags.RiverNo))
        {
            refrechCells(cell.RemoveRivers());
        }

        if (flags.Has(EditorFlags.RoadNo))
        {
            refrechCells(cell.RemoveRoads());
        }

        if (flags.Has(EditorFlags.Drag))
        {
            HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
            if (otherCell)
            {

                if (flags.Has(EditorFlags.RiverYes))
                {
                    refrechCells(otherCell.SetOutgoingRiver(dragDirection));
                }

                if (flags.Has(EditorFlags.RoadYes))
                {
                    refrechCells(otherCell.AddRoad(dragDirection));
                }
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
        flags = flags.Without(EditorFlags.RiverOpts);
        switch (mode)
        {
            case 0:
                flags = flags.With(EditorFlags.RiverIgnore);
                break;
            case 1:
                flags = flags.With(EditorFlags.RiverYes);
                break;
            case 2:
                flags = flags.With(EditorFlags.RiverNo);
                break;
        }
    }

    public void SetRoadMode(int mode)
    {
        flags = flags.Without(EditorFlags.RoadOpts);
        switch (mode)
        {
            case 0:
                flags = flags.With(EditorFlags.RoadIgnore);
                break;
            case 1:
                flags = flags.With(EditorFlags.RoadYes);
                break;
            case 2:
                flags = flags.With(EditorFlags.RoadNo);
                break;
        }
    }

    void OnValidate()
    {
        RegisterEvents();
    }
}