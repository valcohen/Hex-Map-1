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

    private void Update() {
        if (Input.GetMouseButton(0) && 
            !EventSystem.current.IsPointerOverGameObject()
        ) {
            HandleInput();
        }
    }

    void HandleInput() {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            EditCell(hexGrid.GetCell(hit.point));
        }
    }

    void EditCell(HexCell cell) {
        if (applyColor) {
            cell.Color = activeColor;
        }
        if (applyElevation) {
            cell.Elevation = activeElevation;
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
}