using UnityEngine;

public class HexMapCamera : MonoBehaviour {

    public float stickMinZoom, stickMaxZoom;
    public float swivelMinZoom, swivelMaxZoom;

    Transform swivel, stick;
    float zoom = 1f;

    void Awake() {
        swivel  = transform.GetChild(0);
        stick   = swivel.GetChild(0);
    }

    void Update() {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        Debug.Log("zoomDelta: " + zoomDelta);
        if (zoomDelta != 0f) {
            AdjustZoom(zoomDelta);
        }
    }

    void AdjustZoom (float delta) {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }
}
