using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class HexMapEditorSaveLoad : MonoBehaviour
{
    private bool saveModle;
    private HexGrid hexGrid;
    private HexMapCamera hexMapCamera;
    private UIDocument uiDocument;
    public void Open(bool saveModle, HexGrid grid, HexMapCamera hexMapCamera)
    {
        hexGrid = grid;
        this.hexMapCamera = hexMapCamera;
        this.saveModle = saveModle;
        gameObject.SetActive(true);
        uiDocument = GetComponent<UIDocument>();
        registerEvents();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }


    private void registerEvents()
    {
        VisualElement root = uiDocument.rootVisualElement;
        if (root == null)
        {
            return;
        }

        root.Q<Label>("MenuText").text = saveModle ? "Save Map" : "Load Map";
        Button saveLoad = root.Q<Button>("SaveLoadButton");
        saveLoad.text = saveModle ? "Save" : "Load";
        saveLoad.RegisterCallbackOnce<MouseUpEvent>(ent => Action());

        DropdownField field = root.Q<DropdownField>("MapList");
        FillList(field);
        field.RegisterValueChangedCallback(ent => setSelectedPath(ent.newValue));

        root.Q<Button>("deleteMapButton").RegisterCallback<MouseUpEvent>(ent => Delete());

        root.Q<Button>("CancelButton").RegisterCallback<MouseUpEvent>(ent => Close());
    }

    public void Action()
    {
        string path = GetSelectedPath();
        if (path == null)
        {
            return;
        }
        if (saveModle)
        {
            Save(path);
        }
        else
        {
            Load(path);
        }
        Close();
    }

    string GetSelectedPath()
    {
        VisualElement root = uiDocument.rootVisualElement;
        string mapName = root.Q<TextField>("nameInput").value;
        if (mapName.Length == 0)
        {
            return null;
        }
        return Path.Combine(Application.dataPath, mapName + ".map");
    }

    void setSelectedPath(string mapName)
    {
        VisualElement root = uiDocument.rootVisualElement;
        root.Q<TextField>("nameInput").value = mapName;
    }

    void FillList(DropdownField dropdownField)
    {
        string[] paths = Directory.GetFiles(Application.dataPath, "*.map");
        Array.Sort(paths);
        dropdownField.choices.Clear();
        dropdownField.value = "";
        foreach (string path in paths)
        {
            dropdownField.choices.Add(Path.GetFileNameWithoutExtension(path));
        }
    }

    public void Save(string path)
    {
        using (BinaryWriter write = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            int version = 2;
            write.Write(version);
            hexGrid.Save(write);
        }
    }

    public void Load(string path)
    {
        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            int version = reader.ReadInt32();
            if (version <= 2)
            {
                hexGrid.Load(reader, version);
                hexMapCamera.AdjustPosition(0f, 0f);
            }
            else
            {
                Debug.LogWarning($"Unknown map format {version}");
            }
        }
    }

    public void Delete()
    {
        string path = GetSelectedPath();
        if (path == null)
        {
            return;
        }
        if (File.Exists(path))
        {
            File.Delete(path);
            setSelectedPath("");
            DropdownField field = uiDocument.rootVisualElement.Q<DropdownField>("MapList");
            FillList(field);
        }
    }

}