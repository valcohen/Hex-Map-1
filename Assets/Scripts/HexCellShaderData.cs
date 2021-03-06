﻿using System;
using System.Collections.Generic;
using UnityEngine;

// transfers cell-specific data to the GPU to inform rendering
public class HexCellShaderData : MonoBehaviour {

    Texture2D cellTexture;
    Color32[] cellTextureData;

    List<HexCell> transitioningCells = new List<HexCell>();
    const float transitionSpeed = 255f;

    bool needsVisibilityReset;

    public bool ImmediateMode { get; set; }

    public HexGrid Grid { get; set; }

    public void Initalize(int x, int z) {
        if (cellTexture) {
            cellTexture.Resize(x, z);
        }
        else {
            cellTexture = new Texture2D(
                x, z, TextureFormat.RGBA32,
                false   /* mipmaps */,
                true    /* linear color space */
            );
            cellTexture.filterMode = FilterMode.Point;
            cellTexture.wrapMode   = TextureWrapMode.Clamp;
            Shader.SetGlobalTexture("_HexCellData", cellTexture);
        }
        // make texture size available to shader
        Shader.SetGlobalVector(
            "_HexCellData_TexelSize",
            new Vector4(1f / x, 1f / z, x, z)
        );

        if (cellTextureData == null || cellTextureData.Length != x * z) {
            cellTextureData = new Color32[x * z];
        }
        else {
            for (int i = 0; i < cellTextureData.Length; i++) {
                cellTextureData[i] = new Color32(0, 0, 0, 0);
            }
        }

        transitioningCells.Clear();
        enabled = true;
    }

    public void RefreshTerrain (HexCell cell) {
        cellTextureData[cell.Index].a = (byte)cell.TerrainTypeIndex;
        enabled = true;
    }

    public void RefreshVisibility (HexCell cell) {
        int index = cell.Index;
        if (ImmediateMode) {
            cellTextureData[index].r = cell.IsVisible ? (byte)255 : (byte)0;
            cellTextureData[index].g = cell.IsExplored ? (byte)255 : (byte)0;
        }
        else if (cellTextureData[index].b != 255) {
            cellTextureData[index].b = 255;     // cell is in transition
            transitioningCells.Add(cell);
        }
        
        enabled = true;
    }

    void LateUpdate() {
        if (needsVisibilityReset) {
            needsVisibilityReset = false;
            Grid.ResetVisibility();
        }

        int delta = (int)(Time.deltaTime * transitionSpeed);
        // high framerates + low transiion speed could result in delta = 0
        if (delta == 0) {
            delta = 1;
        }
        for (int i = 0; i < transitioningCells.Count; i++) {
            // remove cell from list when transition is finished
            if ( !UpdateCellData(transitioningCells[i], delta) ) {

                // transitioningCells.RemoveAt(i--);

                // optimization to prevent RemoveAt() shifting the list:
                // move the last cell to current index, then remove the last one. 
                transitioningCells[i--] = 
                    transitioningCells[transitioningCells.Count - 1];
                transitioningCells.RemoveAt(transitioningCells.Count - 1);
            }
        }

        cellTexture.SetPixels32(cellTextureData);
        cellTexture.Apply();

        enabled = transitioningCells.Count > 0;
    }

    bool UpdateCellData(HexCell cell, int delta) {
        int     index           = cell.Index;
        Color32 data            = cellTextureData[index];
        bool    stillUpdating   = false;

        // data.g = exploration
        if (cell.IsExplored && data.g < 255) {  // still in transition
            stillUpdating = true;

            int t = data.g + delta;
            data.g = t >= 255 ? (byte)255 : (byte)t;    // don't overflow byte!
        }

        // data.r = visibility
        if (cell.IsVisible) {
            if (data.r < 255) {
                stillUpdating = true;

                int t = data.r + delta;
                data.r = t >= 255 ? (byte)255 : (byte)t;
            }
        }
        else if (data.r > 0) {  // if cell is invisible, decrease R if > 0
            stillUpdating = true;

            int t = data.r - delta;
            data.r = t < 0 ? (byte)0 : (byte)t;         // don't underflow byte!
        }

        if (!stillUpdating) {
            data.b = 0;     // b = cell is in transition
        }

        cellTextureData[index] = data;
        return stillUpdating;
    }

    public void ViewElevationChanged() {
        needsVisibilityReset = true;
        enabled = true;
    }
}
