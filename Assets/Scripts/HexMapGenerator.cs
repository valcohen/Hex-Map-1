using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour {

    public HexGrid grid;

    int cellCount;

    HexCellPriorityQueue searchFrontier;
    int searchFrontierPhase;

    public void GenerateMap (int x, int z) {
        cellCount = x * z;

        grid.CreateMap(x, z);

        if (searchFrontier == null) {
            searchFrontier = new HexCellPriorityQueue();
        }

        RaiseTerrain(7);

        // Modifying adjacent cells sets a cell's search frontier, 
        // which is also used by unit pathfinding. Reset them.
        for (int i = 0; i < cellCount; i++) {
            grid.GetCell(i).SearchPhase = 0;
        }
    }

    void RaiseTerrain (int chunkSize) {
        searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell();
        firstCell.SearchPhase = searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);

        int size = 0;
        while (size < chunkSize && searchFrontier.Count > 0) {
            HexCell current = searchFrontier.Dequeue();
            current.TerrainTypeIndex = 1;
            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor && neighbor.SearchPhase < searchFrontierPhase) {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = 0;
                    neighbor.SearchHeuristic = 0;
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }
        searchFrontier.Clear();
    }

    HexCell GetRandomCell () {
        return grid.GetCell(Random.Range(0, cellCount));
    }
}
