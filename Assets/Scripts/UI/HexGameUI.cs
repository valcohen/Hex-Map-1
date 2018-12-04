using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour {
    public HexGrid grid;

    public void SetEditMode (bool toggle) {
        this.enabled = !toggle;
        grid.ShowUI(!toggle);
        grid.ClearPath();
    }

    HexCell currentCell;

    bool UpdateCurrentCell () {
        HexCell cell = 
            grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));

        if (cell != currentCell) {
            currentCell = cell;
            return true;
        }
        return false;
    }

    HexUnit selectedUnit;


    void Update() {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            if (Input.GetMouseButtonDown(0)) {
                DoSelection();
            }
            else if (selectedUnit) {
                if (Input.GetMouseButton(1))    // RMB
                {
                    DoMove();
                }
                else
                {
                    DoPathFinding();
                }
            }
        }
    }

    void DoSelection() {
        grid.ClearPath();
        UpdateCurrentCell();
        if (currentCell) {
            selectedUnit = currentCell.Unit;
        }
    }

    void DoPathFinding() {
        if (UpdateCurrentCell()) {
            if (currentCell && selectedUnit.IsValidDestination(currentCell)) {
                grid.FindPath(selectedUnit.Location, currentCell, 24);
            } 
            else {
                grid.ClearPath();
            }
        }
    }

    void DoMove () {
        if (grid.HasPath) {
            selectedUnit.Travel(grid.GetPath());
            grid.ClearPath();
        }
    }

}
