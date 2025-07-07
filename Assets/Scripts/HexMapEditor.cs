using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

[Flags]
enum EditorFlags
{
    Nothing = 0,
    ApplyElevation = 0b000000001,
    ApplyColor = 0b000000010,
    Drag = 0b000000100,
    ApplyWaterLevel = 0b000001000,
    ApplyUrbanLevel = 0b000010000,
    ApplyFarmLevel = 0b000100000,
    ApplyPlantLevel = 0b001000000,
    ApplySpecialIndex = 0b010000000,
    EditModel = 0b100000000,

    RiverIgnore = 0b001_000000000,
    RiverYes = 0b010_000000000,
    RiverNo = 0b100_000000000,
    RiverOpts = 0b111_000000000,

    RoadIgnore = 0b001_000_000000000,
    RoadYes = 0b010_000_000000000,
    RoadNo = 0b100_000_000000000,
    RoadOpts = 0b111_000_000000000,

    WallIgnore = 0b001_000_000_000000000,
    WallYes = 0b010_000_000_000000000,
    WallNo = 0b100_000_000_000000000,
    WallOpts = 0b111_000_000_000000000,
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
    private UIDocument sidePanels, newMapPanel;
    [SerializeField]
    private HexMapEditorSaveLoad saveLoad;
    [SerializeField]
    private HexMapCamera hexMapCamera;
    [SerializeField]
    private Material terrainMaterial;
    [SerializeField]
    private HexMapGenerator mapGenerator;
    private int activeTerrianType;
    int elevation;
    int waterLevel;
    int urbanLevel;
    int framLevel;
    int plantLevel;
    EditorFlags flags;
    int brushSize;
    int specialIndex;
    HexDirection dragDirection;
    HexCell previousCell;

    void Awake()
    {
        ShowGrid(false);
        ShowClimate(false);
        SetEditMode(true);
        SelectTerrianType(1);
        RegisterEvents();
    }

    void OnValidate()
    {
        ShowGrid(false);
        ShowClimate(false);
        SetEditMode(true);
        
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        registerSidePanel();
    }

    private void registerNewMapPanel()
    {
        VisualElement root = newMapPanel.rootVisualElement;
        if (root == null)
        {
            return;
        }

        root.Q<Button>("SmallMapButton").RegisterCallback<MouseUpEvent>(ent => CreateSmallMap());
        root.Q<Button>("MediumMapButton").RegisterCallback<MouseUpEvent>(ent => CreateMediumMap());
        root.Q<Button>("LargeMapButton").RegisterCallback<MouseUpEvent>(ent => CreateLargeMap());

        root.Q<Button>("CancelMapButton").RegisterCallback<MouseUpEvent>(ent => CloseNewMapPanel());
    }

    private void registerSidePanel()
    {
        VisualElement root = sidePanels.rootVisualElement;
        if (root == null)
        {
            return;
        }

        root.Q<RadioButtonGroup>("Colors").RegisterValueChangedCallback(change => SelectTerrianType(change.newValue));
        root.Q<RadioButtonGroup>("Colors").value = Array.IndexOf(colors, activeTerrianType) + 1;

        root.Q<Toggle>("ApplyElevation").RegisterValueChangedCallback(change =>
            flags = change.newValue ?
            flags.With(EditorFlags.ApplyElevation) :
            flags.Without(EditorFlags.ApplyElevation)
        );
        root.Q<Toggle>("ApplyElevation").value = flags.Has(EditorFlags.ApplyElevation);

        root.Q<SliderInt>("Elevation").RegisterValueChangedCallback(change => SetElevation(change.newValue));
        root.Q<SliderInt>("Elevation").value = elevation;

        root.Q<Toggle>("ApplyWaterLevel").RegisterValueChangedCallback(change =>
            flags = change.newValue ?
            flags.With(EditorFlags.ApplyWaterLevel) :
            flags.Without(EditorFlags.ApplyWaterLevel)
        );
        root.Q<Toggle>("ApplyWaterLevel").value = flags.Has(EditorFlags.ApplyWaterLevel);

        root.Q<SliderInt>("WaterLevel").RegisterValueChangedCallback(change => SetWaterLevel(change.newValue));
        root.Q<SliderInt>("WaterLevel").value = waterLevel;

        root.Q<SliderInt>("BrushSize").RegisterValueChangedCallback(change => SetBrushSize(change.newValue));
        root.Q<SliderInt>("BrushSize").value = brushSize;

        root.Q<Toggle>("EditerMode").RegisterValueChangedCallback(change => SetEditMode(change.newValue));
        root.Q<Toggle>("ShowGrid").RegisterValueChangedCallback(change => ShowGrid(change.newValue));
        root.Q<Toggle>("ShowClimate").RegisterValueChangedCallback(change => ShowClimate(change.newValue));

        root.Q<RadioButtonGroup>("River").RegisterValueChangedCallback(change => SetRiverMode(change.newValue));
        SetRiverMode(0);

        root.Q<RadioButtonGroup>("Road").RegisterValueChangedCallback(change => SetRoadMode(change.newValue));
        SetRoadMode(0);

        root.Q<RadioButtonGroup>("Wall").RegisterValueChangedCallback(change => SetWallMode(change.newValue));
        SetWallMode(0);

        root.Q<Button>("SaveButton").RegisterCallback<MouseUpEvent>(ent => saveLoad.Open(true, hexGrid, hexMapCamera));
        root.Q<Button>("LoadButton").RegisterCallback<MouseUpEvent>(ent => saveLoad.Open(false, hexGrid, hexMapCamera));


        root.Q<Toggle>("ApplyUrbanLevel").RegisterValueChangedCallback(change =>
            flags = change.newValue ?
            flags.With(EditorFlags.ApplyUrbanLevel) :
            flags.Without(EditorFlags.ApplyUrbanLevel)
        );
        root.Q<Toggle>("ApplyUrbanLevel").value = flags.Has(EditorFlags.ApplyUrbanLevel);

        root.Q<SliderInt>("UrbanLevel").RegisterValueChangedCallback(change => urbanLevel = change.newValue);
        root.Q<SliderInt>("UrbanLevel").value = urbanLevel;

        root.Q<Toggle>("ApplyFarmLevel").RegisterValueChangedCallback(change =>
           flags = change.newValue ?
           flags.With(EditorFlags.ApplyFarmLevel) :
           flags.Without(EditorFlags.ApplyFarmLevel)
       );
        root.Q<Toggle>("ApplyFarmLevel").value = flags.Has(EditorFlags.ApplyFarmLevel);

        root.Q<SliderInt>("FarmLevel").RegisterValueChangedCallback(change => framLevel = change.newValue);
        root.Q<SliderInt>("FarmLevel").value = framLevel;

        root.Q<Toggle>("ApplyPlantLevel").RegisterValueChangedCallback(change =>
           flags = change.newValue ?
           flags.With(EditorFlags.ApplyPlantLevel) :
           flags.Without(EditorFlags.ApplyPlantLevel)
       );
        root.Q<Toggle>("ApplyPlantLevel").value = flags.Has(EditorFlags.ApplyPlantLevel);

        root.Q<SliderInt>("PlantLevel").RegisterValueChangedCallback(change => plantLevel = change.newValue);
        root.Q<SliderInt>("PlantLevel").value = plantLevel;

        root.Q<Toggle>("ApplySpecialIndex").RegisterValueChangedCallback(change =>
           flags = change.newValue ?
           flags.With(EditorFlags.ApplySpecialIndex) :
           flags.Without(EditorFlags.ApplySpecialIndex)
       );
        root.Q<Toggle>("ApplySpecialIndex").value = flags.Has(EditorFlags.ApplySpecialIndex);

        root.Q<SliderInt>("SpecialIndex").RegisterValueChangedCallback(change => specialIndex = change.newValue);
        root.Q<SliderInt>("SpecialIndex").value = specialIndex;

        root.Q<Button>("NewMapButton").RegisterCallback<MouseUpEvent>(ent => OpenNewMapPanel());
    }

    void OpenNewMapPanel()
    {
        newMapPanel.gameObject.SetActive(true);
        registerNewMapPanel();
    }


    void CloseNewMapPanel()
    {
        newMapPanel.gameObject.SetActive(false);
    }

    public bool GenerateMaps
    {
        get
        {
            VisualElement root = newMapPanel.rootVisualElement;
            if (root == null)
            {
                return false;
            }

            return root.Q<Toggle>("ApplyGenerate").value;
        }
    }

    void CreateMap(int x, int z)
    {
        if (GenerateMaps)
        {
            mapGenerator.GenerateMap(x, z);
        }
        else
        {
            hexGrid.CreateMap(x, z);
        }

        hexMapCamera.AdjustPosition(0f, 0f);
        CloseNewMapPanel();
    }

    public void CreateSmallMap()
    {
        CreateMap(20, 15);
    }

    public void CreateMediumMap()
    {
        CreateMap(40, 30);
    }

    public void CreateLargeMap()
    {
        CreateMap(80, 60);
    }

    void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButton(0))
            {
                HandleInput();
            }
            else if (Input.GetKeyDown(KeyCode.U))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    DestroyUnit();
                }
                else
                {
                    CreateUnit();
                }
            }
        }
        else
        {
            previousCell = null;
        }
    }


    void HandleInput()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell)
        {

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

    void CreateUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && !cell.Unit)
        {
            hexGrid.AddUnit(Instantiate(hexGrid.UnitPrefab), cell, UnityEngine.Random.Range(0f, 360f));
        }
    }

    void DestroyUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && cell.Unit)
        {
            hexGrid.RemoveUnit(cell.Unit);
        }
    }

    private HexCell GetCellUnderCursor()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        return hexGrid.GetCell(inputRay);
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

        if (flags.Has(EditorFlags.ApplySpecialIndex) &&
                !cell.HasRiver &&
                !cell.IsUnderwater)
        {
            cell.SpecialIndex = specialIndex;
            cell.RemoveRoads();
            RefreshCellWithDependents(cell);
        }

        if (flags.Has(EditorFlags.ApplyColor))
        {
            cell.TerrainTypeIndex = activeTerrianType;
        }

        if (flags.Has(EditorFlags.ApplyUrbanLevel))
        {
            cell.UrbanLevel = urbanLevel;
            RefreshCellWithDependents(cell);
        }

        if (flags.Has(EditorFlags.ApplyFarmLevel))
        {
            cell.FarmLevel = framLevel;
            RefreshCellWithDependents(cell);
        }

        if (flags.Has(EditorFlags.ApplyPlantLevel))
        {
            cell.PlantLevel = plantLevel;
            RefreshCellWithDependents(cell);
        }

        if (flags.Has(EditorFlags.ApplyElevation))
        {
            int originalViewElevation = cell.ViewElevation;
            cell.Elevation = elevation;
            if (cell.ViewElevation != originalViewElevation)
            {
                hexGrid.ViewElevationChanged = true;
            }
            RefreshCellWithDependents(cell);
            refrechCells(cell.RemoveInvalidRiver());
            for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
            {
                if (cell.HasRoadThroughEdge(d) && cell.GetElevationDifference(d) > 1)
                {
                    refrechCells(cell.RemoveRoad(d));
                }
            }
        }

        if (flags.Has(EditorFlags.ApplyWaterLevel))
        {
            int originalViewElevation = cell.ViewElevation;
            cell.WaterLevel = waterLevel;
            if (cell.ViewElevation != originalViewElevation)
            {
                hexGrid.ViewElevationChanged = true;
            }
            refrechCells(cell);
            refrechCells(cell.RemoveInvalidRiver());
        }

        if (flags.Has(EditorFlags.RiverNo))
        {
            refrechCells(cell.RemoveRivers());
        }

        if (flags.Has(EditorFlags.RoadNo))
        {
            refrechCells(cell.RemoveRoads());
        }

        if (flags.HasNot(EditorFlags.WallIgnore))
        {
            cell.Walled = flags.Has(EditorFlags.WallYes);
            RefreshCellWithDependents(cell);
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
    }

    private void RefreshCellWithDependents(HexCell cell)
    {
        refrechCells(cell);
        refrechCells(cell.Neighbors);
    }

    void refrechCells(HexCell cell)
    {

        HexGridChunk chunk = hexGrid.GetChunk(cell);
        if (chunk)
        {
            chunk.Refresh();
        }
        if (cell.Unit)
        {
            cell.Unit.ValidateLocation();
        }
    }

    void refrechCells(IEnumerable<HexCell> cells)
    {
        foreach (HexCell cell in cells)
        {
            if (!cell)
            {
                continue;
            }

            refrechCells(cell);
        }
    }

    public void SelectTerrianType(int index)
    {
        flags = index > 0 ? flags.With(EditorFlags.ApplyColor) : flags.Without(EditorFlags.ApplyColor);
        if (flags.Has(EditorFlags.ApplyColor))
        {
            activeTerrianType = index - 1;
        }
    }

    public void SetApplyElevation(bool toggle)
    {
        flags = toggle ? flags.With(EditorFlags.ApplyElevation) : flags.Without(EditorFlags.ApplyElevation);
    }

    public void SetElevation(float elevation)
    {
        this.elevation = (int)elevation;
    }

    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }

    public void SetWaterLevel(float waterLevel)
    {
        this.waterLevel = (int)waterLevel;
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

    public void SetWallMode(int mode)
    {
        SetWallMode(mode, EditorFlags.WallOpts, EditorFlags.WallIgnore, EditorFlags.WallYes, EditorFlags.WallNo);
    }

    void SetWallMode(int mode, EditorFlags opts, EditorFlags ignore, EditorFlags yes, EditorFlags no)
    {
        flags = flags.Without(opts);
        switch (mode)
        {
            case 0:
                flags = flags.With(ignore);
                break;
            case 1:
                flags = flags.With(yes);
                break;
            case 2:
                flags = flags.With(no);
                break;
            default: throw new IndexOutOfRangeException($"Invalid mode: {mode}, with flags {opts} {ignore} {yes} {no}");
        }
    }

    public void ShowGrid(bool visible)
    {
        if (visible)
        {
            terrainMaterial.EnableKeyword("_SHOW_GRID");
        }
        else
        {
            terrainMaterial.DisableKeyword("_SHOW_GRID");
        }
    }

    public void ShowClimate(bool visible)
    {
        if (visible)
        {
            terrainMaterial.EnableKeyword("_SHOW_CLIMATE_DATA");
        }
        else
        {
            terrainMaterial.DisableKeyword("_SHOW_CLIMATE_DATA");
        }
    }

    public void SetEditMode(bool toggle)
    {
        flags = toggle ? flags.With(EditorFlags.EditModel) : flags.Without(EditorFlags.EditModel);
    }

}