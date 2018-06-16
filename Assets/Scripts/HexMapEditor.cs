using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {

    public Color[]  colors;
    public HexGrid  hexGrid;

    private Color   activeColor;
    private int     activeElevation;

    bool applyColor;
    bool applyElevation;
    int brushSize;

    private void Awake() {
        SelectColor(-1);
    }

    bool isDrag;
    HexDirection dragDirection;
    HexCell previousCell;

    private void Update() {
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
                Debug.Log("ValidateDrag: " + isDrag);
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

        if (applyColor) {
            cell.Color = activeColor;
        }
        if (applyElevation) {
            cell.Elevation = activeElevation;
        }

        if (riverMode == OptionalToggle.No) {
            cell.RemoveRiver();
        } else if ( isDrag && riverMode == OptionalToggle.Yes) {
            HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
            if (otherCell) {
                otherCell.SetOutgoingRiver(dragDirection);
            }
        }
    }

    public void SelectColor(int index) {
        applyColor = index >= 0;
        if (applyColor) {
            activeColor = colors[index];
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
    OptionalToggle riverMode;

    public void SetRiverMode (int mode) {
        riverMode = (OptionalToggle)mode;
    }
}