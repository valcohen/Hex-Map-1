﻿using UnityEngine;
using UnityEngine.UI;

public class HexGridChunk : MonoBehaviour {

    HexCell[] cells;

    HexMesh hexMesh;
    Canvas gridCanvas;

    void Awake () {
        gridCanvas  = GetComponentInChildren<Canvas>();
        hexMesh     = GetComponentInChildren<HexMesh>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
    }

    void Start() {
        hexMesh.Triangulate(cells);
    }
}
