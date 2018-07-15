using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {


    public int cellCountX = 20;
    public int cellCountZ = 15;
    int chunkCountX, chunkCountZ;

    public HexGridChunk chunkPrefab;

    public HexCell  cellPrefab;
    public Text     cellLabelPrefab;

    public Texture2D noiseSource;

    HexGridChunk[]  chunks;
    HexCell[]       cells;

    public int seed;

    void Awake() {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        CreateMap(cellCountX, cellCountZ);
    }

    void OnEnable() {
        if (!HexMetrics.noiseSource) {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
        }

    }

    public bool CreateMap (int x, int z) {
        if (
                x <= 0 || x % HexMetrics.chunkSizeX != 0
            ||  x <= 0 || z % HexMetrics.chunkSizeZ != 0
        ) {
            UnityEngine.Debug.LogError("Unsupported map size: " + x + "," + z 
                           + " does not divide into chunk size "
                           + HexMetrics.chunkSizeX + ", " 
                           + HexMetrics.chunkSizeZ + "."
            );
            return false;
        }

        if (chunks != null) {
            for (int i = 0; i < chunks.Length; i++) {
                Destroy(chunks[i].gameObject);
            }
        }

        cellCountX = x;
        cellCountZ = z;
        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();

        return true;
    }

    void CreateChunks () {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++) {
            for (int x = 0; x < chunkCountX; x++) {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(this.transform);
            }
        }
    }

    void CreateCells () {
        cells = new HexCell[cellCountZ * cellCountX];

        for (int z = 0, i = 0; z < cellCountZ; z++) {
            for (int x = 0; x < cellCountX; x++) {
                CreateCell(x, z, i++);
            }
        }
	}

    public HexCell GetCell (Vector3 position) {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return cells[index];
    }

    public HexCell GetCell (HexCoordinates coordinates) {
        int z = coordinates.Z;
        if (z < 0 || z >= cellCountZ) {
            return null;
        }
        int x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX) {
            return null;
        }
        return cells[x + z * cellCountX];
    }

    public void ShowUI (bool visible) {
        for (int i = 0; i < chunks.Length; i++) {
            chunks[i].ShowUI(visible);
        }
    }

    void CreateCell(int x, int z, int i) {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        // cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.name = "Cell " + cell.coordinates.ToString();

        if (x > 0) {    // skip 1st col, set West neighbor
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0) {    // skip first row, as it has no lower neighbors
            // even rows
            if ((z & 1) == 0) {    // set SouthEast neighbor
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0) {       // skip 1st col, set SouthWest neighbor
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            // odd rows
            else {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1) {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }

        Text label = Instantiate<Text>(cellLabelPrefab);
        // label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);

        cell.uiRect = label.rectTransform;

        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    void AddCellToChunk (int x, int z, HexCell cell) {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    /*
     * IO
     */

    public void Save(BinaryWriter writer) {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);

        for (int i = 0; i < cells.Length; i++) {
            cells[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader, int header) {
        // StopAllCoroutines();        // stop distance searches

        int x = 20, z = 15;         // default values for version 0
        if (header >= 1) {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        // skip creating new map if loading one of the current size
        if (x != cellCountX || z != cellCountZ) {
            if (!CreateMap(x, z)) {
                return;
            }
        }

        for (int i = 0; i < cells.Length; i++) {
            cells[i].Load(reader);
        }

        for (int i = 0; i < chunks.Length; i++) {
            chunks[i].Refresh();
        }
    }

    /*
     * Distances
     */

    HexCellPriorityQueue searchFrontier;

    public void FindPath (HexCell fromCell, HexCell toCell, int speed) {
        // StopAllCoroutines();
        // StartCoroutine(Search(fromCell, toCell, speed));
        Search(fromCell, toCell, speed);
    }

    // signature for use with coroutines:
    // IEnumerator Search(HexCell fromCell, HexCell toCell, int speed)
    void Search (HexCell fromCell, HexCell toCell, int speed) {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        // reset
        if (searchFrontier == null) {
            searchFrontier = new HexCellPriorityQueue();
        }
        else {
            searchFrontier.Clear();
        }

        for (int i = 0; i < cells.Length; i++) {
            cells[i].Distance = int.MaxValue;   // max = cell has not been visited
            cells[i].SetLabel(null);
            cells[i].DisableHighlight();
        }
        fromCell.EnableHighlight(Color.blue);
        toCell.EnableHighlight(Color.red);


        // var delay    = new WaitForSeconds(1 / 60f);  // use with coroutines
        int cellsProcessed = 1;

        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);

        while (searchFrontier.Count > 0) {
            // yield return delay;  // use with coroutines
            HexCell current = searchFrontier.Dequeue();

            if (current == toCell) {    // found it! done.
                current = current.PathFrom;
                while (current != fromCell) {
                    current.EnableHighlight(Color.white);
                    current = current.PathFrom;
                }
                break;
            }

            int currentTurn = current.Distance / speed;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = current.GetNeighbor(d);

                if (neighbor == null) {
                    continue;
                }
                if (neighbor.IsUnderwater) { 
                    continue;
                }
                HexEdgeType edgeType = current.GetEdgeType(neighbor);
                if ( edgeType == HexEdgeType.Cliff) {
                    continue;
                }
                int moveCost;
                // road travel costs 1
                if (current.HasRoadThroughEdge(d)) {
                    moveCost = 1;
                }
                // don't allow travel thru walls
                else if (current.Walled != neighbor.Walled) {
                    continue;
                }
                // offroad flats cost 5, everything else costs 10
                else {
                    moveCost = (edgeType == HexEdgeType.Flat) ? 5 : 10;

                    // slow own when moving thru features
                    moveCost += neighbor.UrbanLevel 
                              + neighbor.FarmLevel
                              + neighbor.PlantLevel
                              + neighbor.SpecialIndex;
                }

                int distance = current.Distance + moveCost;
                int turn = distance / speed;
                if (turn > currentTurn) {
                    // eat up all remaining movement points
                    distance = turn * speed + moveCost;
                }

                // not yet visited
                if (neighbor.Distance == int.MaxValue) {
                    neighbor.Distance = distance;
                    neighbor.SetLabel(turn.ToString());
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic =
                        neighbor.coordinates.DistanceTo(toCell.coordinates);
                    searchFrontier.Enqueue(neighbor);
                }
                // update if we found a quicker path
                else if (distance < neighbor.Distance) {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.SetLabel(distance.ToString());
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }

                cellsProcessed++;
                // UnityEngine.Debug.Log("Frontier count: " +  frontier.Count);
            }
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log("Search complete: " + cellsProcessed 
                              + " cells in " + stopwatch.Elapsed);

    }
}
