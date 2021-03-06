using System;
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
    List<HexUnit> units = new List<HexUnit>();

    public HexGridChunk chunkPrefab;

    public HexCell  cellPrefab;
    public Text     cellLabelPrefab;

    public Texture2D noiseSource;

    HexGridChunk[]  chunks;
    HexCell[]       cells;

    public int seed;

    public HexUnit unitPrefab;

    HexCellShaderData cellShaderData;

    void Awake() {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        HexUnit.unitPrefab = unitPrefab;
        cellShaderData = gameObject.AddComponent<HexCellShaderData>();
        cellShaderData.Grid = this;
        CreateMap(cellCountX, cellCountZ);
    }

    void OnEnable() {
        if (!HexMetrics.noiseSource) {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexUnit.unitPrefab = unitPrefab;
            ResetVisibility();
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

        ClearPath();
        ClearUnits();
        if (chunks != null) {
            for (int i = 0; i < chunks.Length; i++) {
                Destroy(chunks[i].gameObject);
            }
        }

        cellCountX = x;
        cellCountZ = z;
        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
        cellShaderData.Initalize(cellCountX, cellCountZ);

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

    public HexCell GetCell(int xOffset, int zOffset) {
        return cells[xOffset + zOffset * cellCountX];
    }

    public HexCell GetCell(int cellIndex) {
        return cells[cellIndex];
    }

    public HexCell GetCell(Ray ray) {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            return GetCell(hit.point);
        }
        return null;
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
        cell.Index = i;
        cell.ShaderData = cellShaderData;

        // make map edges unexplorable & therefore hidden, so they fade into bg:
        cell.Explorable =  x > 0 && x < cellCountX - 1
                        && z > 0 && z < cellCountZ - 1;

        cell.name = "Cell " + cell.coordinates.ToString();

        // assign neighbor cells
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

        writer.Write(units.Count);
        for (int i = 0; i < units.Count; i++) {
            units[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader, int header) {
        // StopAllCoroutines();        // stop distance searches

        ClearPath();
        ClearUnits();

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

        // prevent visibility transitions on load
        bool originalImmediateMode = cellShaderData.ImmediateMode;
        cellShaderData.ImmediateMode = true;

        for (int i = 0; i < cells.Length; i++) {
            cells[i].Load(reader, header);
        }

        for (int i = 0; i < chunks.Length; i++) {
            chunks[i].Refresh();
        }

        if (header >= 2) {
            int unitCount = reader.ReadInt32();
            for (int i = 0; i < unitCount; i++) {
                HexUnit.Load(reader, this);
            }
        }

        cellShaderData.ImmediateMode = originalImmediateMode;
    }

    /*
     * Distances
     */

    HexCellPriorityQueue searchFrontier;
    int searchFrontierPhase;
    HexCell currentPathFrom, currentPathTo;
    bool currentPathExists;
    int cellsProcessed;

    public void FindPath (HexCell fromCell, HexCell toCell, HexUnit unit) {
        // StopAllCoroutines();
        // StartCoroutine(Search(fromCell, toCell, unit));

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        ClearPath();
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = Search(fromCell, toCell, unit);
        ShowPath(unit.Speed);

        stopwatch.Stop();
        /* UnityEngine.Debug.Log(
            string.Format("Search complete: {0} cells in {1} milliseconds",
                          cellsProcessed,
                          stopwatch.ElapsedMilliseconds)
        );
        */
    }

    // signature for use with coroutines:
    // IEnumerator Search(HexCell fromCell, HexCell toCell, HexUnit unit)
    bool Search (HexCell fromCell, HexCell toCell, HexUnit unit) {
        int speed = unit.Speed;

        // reset
        searchFrontierPhase += 2;
        if (searchFrontier == null) {
            searchFrontier = new HexCellPriorityQueue();
        }
        else {
            searchFrontier.Clear();
        }

        // var delay    = new WaitForSeconds(1 / 60f);  // use with coroutines
        cellsProcessed = 1;

        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);

        while (searchFrontier.Count > 0) {
            // yield return delay;  // use with coroutines

            // take cell out of frontier
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;

            if (current == toCell) {    // found it! done.
                return true;
            }

            int currentTurn = (current.Distance - 1) / speed;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = current.GetNeighbor(d);

                if (
                        neighbor == null
                    // skip cells that have already been removed from frontier
                    ||  neighbor.SearchPhase > searchFrontierPhase
                ) {
                    continue;
                }

                if (!unit.IsValidDestination(neighbor)) {
                    continue;
                }

                int moveCost = unit.GetMoveCost(current, neighbor, d);
                if (moveCost < 0) {     // can't move
                    continue;
                }

                int distance = current.Distance + moveCost;
                int turn = (distance - 1) / speed;
                if (turn > currentTurn) {
                    // eat up all remaining movement points
                    distance = turn * speed + moveCost;
                }

                // not yet visited
                if (neighbor.SearchPhase < searchFrontierPhase) {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    // neighbor.SetLabel(turn.ToString()); // use with coroutines to display progress
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic =
                        neighbor.coordinates.DistanceTo(toCell.coordinates);
                    searchFrontier.Enqueue(neighbor);
                }
                // update if we found a quicker path
                else if (distance < neighbor.Distance) {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    // neighbor.SetLabel(distance.ToString()); // use with coroutines to display progress
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }

                cellsProcessed++;
                // UnityEngine.Debug.Log("Frontier count: " +  frontier.Count);
            }
        }
        return false;
    }

    public bool HasPath {
        get { return currentPathExists; }
    }

    void ShowPath (int speed) {
        if (currentPathExists) {
            HexCell current = currentPathTo;
            while (current != currentPathFrom) {
                int turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.white);
                current = current.PathFrom;
            }
            currentPathFrom.EnableHighlight(Color.blue);
            currentPathTo.EnableHighlight(Color.red);
        }
    }

    public void ClearPath() {
        if (currentPathExists) {
            HexCell current = currentPathTo;
            while (current != currentPathFrom) {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }
            if (currentPathFrom != null)    // TODO: ugly hack; why is this null when currentPathExists is TRUE?
            {
                currentPathFrom.DisableHighlight();
            }
            currentPathExists = false;
        }
        currentPathFrom = currentPathTo = null;
    }

    public List<HexCell> GetPath () {
        if (!currentPathExists) { return null; }
        List<HexCell> path = ListPool<HexCell>.Get();
        for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom) {
            path.Add(c);
        }
        path.Add(currentPathFrom);
        path.Reverse();
        return path;
    }

    void ClearUnits () {
        for (int i = 0; i < units.Count; i++) {
            units[i].Die();
        }
        units.Clear();
    }

    public void AddUnit (HexUnit unit, HexCell location, float orientation) {
        units.Add(unit);
        unit.Grid = this;
        unit.transform.SetParent(this.transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }

    public void RemoveUnit (HexUnit unit) {
        units.Remove(unit);
        unit.Die();
    }

    List<HexCell> GetVisibleCells (HexCell fromCell, int range) {
        List<HexCell> visibleCells = ListPool<HexCell>.Get();

        // reset
        searchFrontierPhase += 2;
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else
        {
            searchFrontier.Clear();
        }

        // var delay    = new WaitForSeconds(1 / 60f);  // use with coroutines
        cellsProcessed = 1;

        range += fromCell.ViewElevation;
        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);

        HexCoordinates fromCoordinates = fromCell.coordinates;
        while (searchFrontier.Count > 0)
        {
            // yield return delay;  // use with coroutines

            // take cell out of frontier
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;

            visibleCells.Add(current);

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = current.GetNeighbor(d);

                if (
                        neighbor == null
                    // skip cells that have already been removed from frontier
                    || neighbor.SearchPhase > searchFrontierPhase
                    || !neighbor.Explorable
                ) {
                    continue;
                }

                int distance = current.Distance + 1;
                if (    distance + neighbor.ViewElevation > range
                    // only use shortest path for visibility:
                    ||  distance > fromCoordinates.DistanceTo(neighbor.coordinates)
                ) { 
                    continue; 
                }

                // not yet visited
                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    searchFrontier.Enqueue(neighbor);
                }
                // update if we found a quicker path
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    searchFrontier.Change(neighbor, oldPriority);
                }

                cellsProcessed++;
                // UnityEngine.Debug.Log("Frontier count: " +  frontier.Count);
            }
        }
        return visibleCells;
    }

    public void IncreaseVisibility (HexCell fromCell, int range) {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++) {
            cells[i].IncreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }

    public void DecreaseVisibility (HexCell fromCell, int range) {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++) {
            cells[i].DecreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }

    public void ResetVisibility() {
        for (int i = 0; i < cells.Length; i++) {
            cells[i].ResetVisibility();
        }
        for (int i = 0; i < units.Count; i++) {
            HexUnit unit = units[i];
            IncreaseVisibility(unit.Location, unit.VisionRange);
        }
    }
}
