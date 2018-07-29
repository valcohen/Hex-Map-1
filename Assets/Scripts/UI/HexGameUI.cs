using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour {
    public HexGrid grid;

    public void SetEditMode (bool toggle) {
        this.enabled = !toggle;
        grid.ShowUI(!toggle);
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

    void DoSelection() {
        UpdateCurrentCell();
        if (currentCell) {
            selectedUnit = currentCell.Unit;
        }
    }

    void Update() {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            if (Input.GetMouseButtonDown(0)) {
                DoSelection();
            }
        }
    }
}
