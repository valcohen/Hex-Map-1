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

    public void AddFeature(Vector3 position) {
        Transform instance = Instantiate(featurePrefab);

        // raise the default feature cube so it sits on the ground;
        // not necessary for meshes whose origin is already at the bottom
        position.y += instance.localScale.y * 0.5f; 
        instance.localPosition = HexMetrics.Perturb(position);
        instance.SetParent(container, false);
    }
}
