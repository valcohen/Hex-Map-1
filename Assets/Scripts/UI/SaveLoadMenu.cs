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

    public void Action () {
        string path = GetSelectedPath();
        if (path == null) { return; }

        if (saveMode) {
            Save(path);
        }
        else {
            Load(path);
        }
        Close();
    }

    string GetSelectedPath () {
        string mapName = nameInput.text.Trim();
        if (mapName.Length == 0) {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    void Save (string path) {
        Debug.Log("Save " + path);

        using (BinaryWriter writer =
            new BinaryWriter(File.Open(path, FileMode.Create))
        )
        {
            writer.Write(1);        // file format version number
            hexGrid.Save(writer);
        }

    }

    void Load (string path) {
        if (!File.Exists(path)) {
            Debug.LogError("File does not exist: " + path);
            return;
        }

        Debug.Log("Load " + path);

        using (BinaryReader reader =
               new BinaryReader(File.OpenRead(path))
        )
        {
            int header = reader.ReadInt32();     // read file format version number
            if (header <= 1)
            {
                hexGrid.Load(reader, header);
                HexMapCamera.ValidatePosition();
            }
            else
            {
                Debug.LogWarning("Unkown map format: " + header);
            }
        }

    }


}
