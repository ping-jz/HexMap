using System;
using System.Collections.Generic;
using UnityEngine;

public class HexCellShaderData : MonoBehaviour
{
    Texture2D cellTexture;
    Color32[] cellTextureData;
    List<int> transitioningCellIndices = new List<int>();
    const float transitionSpeed = 255f;
    HexGrid grid;

    public bool ImmediateMode { get; set; }

    public void Initialize(HexGrid grid, int x, int z)
    {
        if (cellTexture)
        {
            cellTexture.Reinitialize(x, z);
        }
        else
        {
            cellTexture = new Texture2D(
                x, z, TextureFormat.RGBA32, false, true
            );
            cellTexture.filterMode = FilterMode.Point;
            cellTexture.wrapModeV = TextureWrapMode.Clamp;
            cellTexture.wrapModeU = TextureWrapMode.Repeat;
            Shader.SetGlobalTexture("_HexCellData", cellTexture);
        }
        Shader.SetGlobalVector(
            "_HexCellData_TexelSize",
            new Vector4(1f / x, 1f / z, x, z)
        );

        if (cellTextureData == null || cellTextureData.Length != x * z)
        {
            cellTextureData = new Color32[x * z];
        }
        else
        {
            for (int i = 0; i < cellTextureData.Length; i++)
            {
                cellTextureData[i] = new Color32(0, 0, 0, 0);
            }
        }

        transitioningCellIndices.Clear();
        this.grid = grid;
        enabled = true;
    }

    public void RefreshTerrain(HexCell cell)
    {
        cellTextureData[cell.Index].a = (byte)cell.TerrainTypeIndex;
        enabled = true;
    }

    public void RefreshVisibility(HexCell cell)
    {
        int index = cell.Index;
        if (ImmediateMode)
        {
            cellTextureData[index].r = grid.IsCellVisible(cell.Index) ? (byte)255 : (byte)0;
            cellTextureData[index].g = cell.IsExplored ? (byte)255 : (byte)0;
        }
        else if (cellTextureData[index].b != 255)
        {
            cellTextureData[index].b = 255;
            transitioningCellIndices.Add(cell.Index);
        }

        enabled = true;
    }

    public void SetMapData(HexCell cell, float data)
    {
        //for debug only
        cellTextureData[cell.Index].b =
            data < 0f ? (byte)0 : (data < 1f ? (byte)(data * 254f) : (byte)254);
        enabled = true;
    }

    void LateUpdate()
    {
        int delta = Math.Max(1, (int)(Time.deltaTime * transitionSpeed));
        for (int i = 0; i < transitioningCellIndices.Count; i++)
        {
            if (!UpdateCellData(transitioningCellIndices[i], delta))
            {
                transitioningCellIndices[i] = transitioningCellIndices[transitioningCellIndices.Count - 1];
                transitioningCellIndices.RemoveAt(transitioningCellIndices.Count - 1);
                i -= 1;
            }
        }
        cellTexture.SetPixels32(cellTextureData);
        cellTexture.Apply();
        enabled = transitioningCellIndices.Count > 0;
    }

    bool UpdateCellData(int index, int delta)
    {
        HexCell cell = grid.GetCell(index);
        Color32 data = cellTextureData[index];
        bool updating = false;

        if (cell.IsExplored && data.g < 255)
        {
            updating = true;
            int t = data.g + delta;
            data.g = (byte)Math.Min(t, 255);
        }

        if (grid.IsCellVisible(index) && data.r < 255)
        {
            updating = true;
            int t = data.r + delta;
            data.r = (byte)Math.Min(t, 255);
        }
        else if (data.r > 0)
        {
            updating = true;
            int t = data.r - delta;
            data.r = (byte)Math.Max(t, 0);
        }

        if (!updating)
        {
            data.b = 0;
        }
        cellTextureData[index] = data;
        return updating;
    }
}