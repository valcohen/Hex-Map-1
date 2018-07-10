using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {

    public HexGrid  hexGrid;

    int activeElevation;
    int activeWaterLevel;
    int activeUrbanLevel, activeFarmLevel, activePlantLevel, activeSpecialIndex;
    int activeTerrainTypeIndex;

    bool applyElevation = true;
    bool applyWaterLevel = true;
    bool applyUrbanLevel, applyFarmLevel, applyPlantLevel, applySpecialIndex;

    int brushSize;

    bool isDrag;
    HexDirection dragDirection;
    HexCell previousCell;

    void Update() {
        if (Input.GetMouseButton(0) && 
            !EventSystem.current.IsPointerOverGameObject()
        ) {
            HandleInput();
        } else {
            previousCell = null;
        }
    }

    void HandleInput() {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            HexCell currentCell = hexGrid.GetCell(hit.point);

            if (previousCell && previousCell != currentCell) {
                ValidateDrag(currentCell);
            } else {
                isDrag = false;
            }

            EditCells(currentCell);
            previousCell = currentCell;
        } else {
            previousCell = null;
        }
    }

    void ValidateDrag (HexCell currentCell) {
        for (
            dragDirection = HexDirection.NE;
            dragDirection <= HexDirection.NW;
            dragDirection++
        ) {
            if (previousCell.GetNeighbor(dragDirection) == currentCell) {
                isDrag = true;
                return;
            }
        }
        isDrag = false;
    }

    public void Save () {
        string path = Path.Combine(Application.persistentDataPath, "test.map");
        Debug.Log("Save " + path);

        using (BinaryWriter writer = 
            new BinaryWriter(File.Open(path, FileMode.Create))
        ) {
            hexGrid.Save(writer);
        }

    }

    public void Load () {
        string path = Path.Combine(Application.persistentDataPath, "test.map");
        Debug.Log("Load " + path);

        using (BinaryReader reader =
               new BinaryReader(File.OpenRead(path))
        ) {
            hexGrid.Load(reader);
        }

    }

    /*
     * axes          
     * x: --    \  / y
     * y: /   ___\/___ x
     * z: \      /\
     *          /  \ z
     *
     *                 / \     / \     / \     / \
     *               / -3  \ / -2  \ / -1  \ /  0  \
     *              |   0   |  -1   |  -2   |  -3   |
     *             / \  3  /.\  3  /.\  3  /.\  3  / \
     *           / -3  \ / -2 .\ / -1 .\ /. 0 .\ /  1  \
     *          |   1   |.. 0 ..|. -1 ..|. -2 ..|  -3   |
     *         / \  2  /.\. 2 ./ \. 2 ./ \. 2 ./.\  2  / \
     *       / -3  \ / -2 .\./ -1  \./  0  \./. 1 .\ /  2  \
     *      |   2   |.. 1 ..|   0   |  -1   |. -2 ..|  -3   |
     *     / \  1  /.\. 1 ./ \  1  /.\  1  / \. 1 ./ \  1  / \
     *   / -3  \ / -2 .\./ -1  \ /. 0 .\ /  1  \./. 2 .\ /  3  \
     *  |   3   |.. 2 ..|   1   |.. 0 ..|  -1   |. -2 ..|  -3   |
     *   \  0  / \. 0 ./.\  0  / \. 0 ./ \  0  / \. 0 ./ \  0  /
     *     \ / -2  \./ -1 .\ /  0  \./  1  \ /. 2 .\ /  3  \ /
     *      |   3   |.. 2 ..|   1   |   0   |. -1 ..|  -2   |
     *       \ -1  / \ -1 ./.\ -1  /.\ -1  /.\ -1 ./ \ -1  /
     *         \ / -1  \./. 0 .\ /. 1 .\ /. 2 .\ /  3  \ /
     *          |   3   |.. 2 ..|.. 1 ..|.. 0 ..|  -1   |
     *           \ -2  / \ -2 ./ \ -2 ./ \ -2 ./ \ -2  /
     *             \ /  0  \./  1  \./  2  \./  3  \ /
     *              |   3   |   2   |   1   |   0   |
     *               \ -3  / \ -3  / \ -3  / \ -3  /
     *                 \ /     \ /     \ /     \ /
     */
    void EditCells(HexCell center) {
        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;

        // paint bottom half of brush
        // min Z defines row 0
        for (int row = 0, z = centerZ - brushSize; z <= centerZ; z++, row++) {
            // bottom row first cell has same X as center cell
            // bottom row last cell X = center.X + radius (brush size)
            for (int x = centerX - row; x <= centerX + brushSize; x++) {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
        // paint top half, skip center row
        for (int row = 0, z = centerZ + brushSize; z > centerZ; z--, row++) {
            for (int x = centerX - brushSize; x <= centerX + row; x++) {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    void EditCell(HexCell cell) {
        if (cell == null) { return; }

        if (activeTerrainTypeIndex >= 0) {
            cell.TerrainTypeIndex = activeTerrainTypeIndex;
        }

        if (applyElevation) {
            cell.Elevation = activeElevation;
        }
        if (applyWaterLevel) {
            cell.WaterLevel = activeWaterLevel;
        }
        if (applySpecialIndex) {
            cell.SpecialIndex = activeSpecialIndex;
        }
        if (applyUrbanLevel) {
            cell.UrbanLevel = activeUrbanLevel;
        }
        if (applyFarmLevel) {
            cell.FarmLevel = activeFarmLevel;
        }
        if (applyPlantLevel) {
            cell.PlantLevel = activePlantLevel;
        }
        if (riverMode == OptionalToggle.No) {
            cell.RemoveRiver();
        } 
        if (roadMode == OptionalToggle.No) {
            cell.RemoveRoads();
        }
        if (walledMode != OptionalToggle.Ignore) {
            cell.Walled = walledMode == OptionalToggle.Yes;
        }

        if ( isDrag ) {
            HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
            if (otherCell) {
                if (riverMode == OptionalToggle.Yes) {
                    otherCell.SetOutgoingRiver(dragDirection);
                }
                if (roadMode == OptionalToggle.Yes) {
                    otherCell.AddRoad(dragDirection);
                }
            }
        }
    }

    public void SetElevation(float elevation) {
        activeElevation = (int)elevation;
    }

    public void SetApplyElevation(bool toggle) {
        applyElevation = toggle;
    }

    public void SetBrushSize(float size) {
        brushSize = (int)size;
    }

    public void ShowUI (bool visible) {
        hexGrid.ShowUI(visible);
    }

    enum OptionalToggle {
        Ignore, Yes, No
    }
    OptionalToggle riverMode, roadMode, walledMode;

    public void SetRiverMode (int mode) {
        riverMode = (OptionalToggle)mode;
    }

    public void SetRoadMode (int mode) {
        roadMode = (OptionalToggle)mode;
    }

    public void SetWalledMode (int mode) {
        walledMode = (OptionalToggle)mode;
    }

    public void SetApplyWaterLevel (bool toggle) {
        applyWaterLevel = toggle;
    }

    public void SetWaterLevel (float level) {
        activeWaterLevel = (int)level;
    }

    public void SetApplyUrbanLevel (bool toggle) {
        applyUrbanLevel = toggle;
    }

    public void SetUrbanLevel (float level) {
        activeUrbanLevel = (int)level;
    }

    public void SetApplyFarmLevel(bool toggle)
    {
        applyFarmLevel = toggle;
    }

    public void SetFarmLevel(float level)
    {
        activeFarmLevel = (int)level;
    }

    public void SetApplyPlantLevel(bool toggle)
    {
        applyPlantLevel = toggle;
    }

    public void SetPlantLevel(float level)
    {
        activePlantLevel = (int)level;
    }

    public void SetApplySpecialInde (bool toggle) {
        applySpecialIndex = toggle;
    }

    public void SetSpecialIndex (float index) {
        activeSpecialIndex = (int)index;
    }

    public void SetTerrainTypeInde (int index) {
        activeTerrainTypeIndex = index;
    }
}