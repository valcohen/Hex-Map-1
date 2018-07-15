using UnityEngine;

public class HexUnit : MonoBehaviour {
    
    public HexCell Location {
        get {
            return location;
        }
        set {
            location = value;
            value.Unit = this;
            transform.localPosition = value.Position;
        }
    }

    HexCell location;


    public float Orientation {
        get {
            return orientation;
        }
        set {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    float orientation;
}
