﻿using UnityEngine;

// transfers cell-specific data to the GPU to inform rendering
public class HexCellShaderData : MonoBehaviour {
    Texture2D cellTexture;
    Color32[] cellTextureData;

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
        }

        if (cellTextureData == null || cellTextureData.Length != x * z) {
            cellTextureData = new Color32[x * z];
        }
        else {
            for (int i = 0; i < cellTextureData.Length; i++) {
                cellTextureData[i] = new Color32(0, 0, 0, 0);
            }
        }

        enabled = true;
    }

    public void RefreshTerrain (HexCell cell) {
        cellTextureData[cell.Index].a = (byte)cell.TerrainTypeIndex;
        enabled = true;
    }

    void LateUpdate() {
        cellTexture.SetPixels32(cellTextureData);
        cellTexture.Apply();
        enabled = false;
    }
}