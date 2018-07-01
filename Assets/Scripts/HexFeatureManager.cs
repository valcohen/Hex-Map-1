using UnityEngine;

public class HexFeatureManager : MonoBehaviour {

    public Transform featurePrefab;

    Transform container;

    public void Clear () {
        if (container) {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
    }

    public void Apply () {}

    public void AddFeature(HexCell cell,  Vector3 position) {
        HexHash hash = HexMetrics.SampleHashGrid(position);
        if (hash.a >= cell.UrbanLevel * 0.25f) { return; }

        Transform instance = Instantiate(featurePrefab);

        // raise the default feature cube so it sits on the ground;
        // not necessary for meshes whose origin is already at the bottom
        position.y += instance.localScale.y * 0.5f; 
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.b, 0f);
        instance.SetParent(container, false);
    }
}
