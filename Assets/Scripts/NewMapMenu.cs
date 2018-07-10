using UnityEngine;

public class NewMapMenu : MonoBehaviour {

    public HexGrid hexGrid;

    public void Open () {
        gameObject.SetActive(true);
    }

    public void Close () {
        gameObject.SetActive(false);
    }
}
