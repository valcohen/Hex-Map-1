using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {

    public HexGrid  hexGrid;
    public Material terrainMaterial;

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
    HexCell previousCell, searchFromCell, searchToCell;

    bool editMode;

    void Awake() {
        terrainMaterial.DisableKeyword("GRID_ON");
    }

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
            if (editMode) {
                EditCells(currentCell);
            }
            else if (   Input.GetKey(KeyCode.LeftShift) 
                     && searchToCell != currentCell
            ) {
                if (searchFromCell != currentCell) {
                    if (searchFromCell) {
                        searchFromCell.DisableHighlight();
                    }
                    searchFromCell = currentCell;
                    searchFromCell.EnableHighlight(Color.blue);
                    if (searchToCell) {
                        hexGrid.FindPath(searchFromCell, searchToCell, 24);
                    }
                }
            }
            else if (searchFromCell && searchFromCell != currentCell) {
                if (searchToCell != currentCell) {
                    searchToCell = currentCell;
                    hexGrid.FindPath(searchFromCell, searchToCell, 24);
                }
            }

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

    public void ShowGrid (bool visible) {
        if (visible) {
            terrainMaterial.EnableKeyword("GRID_ON");
        }
        else {
            terrainMaterial.DisableKeyword("GRID_ON");
        }
    }

    public void SetEditMode (bool toggle) {
        editMode = toggle;
        hexGrid.ShowUI(!toggle);    // hide labels when in edit mode, else show them
    }
}