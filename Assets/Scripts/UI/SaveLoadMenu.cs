using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SaveLoadMenu : MonoBehaviour {

    public Text menuLabel, actionButtonLabel;
    public InputField nameInput;

    public HexGrid hexGrid;

    bool saveMode;

    public void Open (bool saveMode) {
        this.saveMode = saveMode;

        if (saveMode) {
            menuLabel.text = "Save Map";
            actionButtonLabel.text = "Save";
        }
        else {
            menuLabel.text = "Load Map";
            actionButtonLabel.text = "Load";
        }

        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close () {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    string GetSelectedPath () {
        string mapName = nameInput.text.Trim();
        if (mapName.Length == 0) {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

}
