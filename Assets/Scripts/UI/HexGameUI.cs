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
                Debug.Log("selecting...");
                DoSelection();
            }
            else if (selectedUnit) {
                DoPathFinding();
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
            if (currentCell) {
                grid.FindPath(selectedUnit.Location, currentCell, 24);
            } 
            else {
                grid.ClearPath();
            }
        }
    }
}
