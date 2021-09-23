using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class GroundSettings : ScriptableObject {
    public event System.Action OnValuesUpdated;

    public Vector3 albedo = Vector3.one * 0.5f;
    public Vector3 specular = Vector3.one * 0.05f;
    [Range(0, 1)]
    public float smoothness = 0.2f;
    public Vector3 emission = Vector3.zero;

    private void OnValidate() {
        ClampValues();
        if (OnValuesUpdated != null) {
            OnValuesUpdated();
        }
    }

    private void ClampValues() {
        albedo.x = Mathf.Clamp01(albedo.x);
        albedo.y = Mathf.Clamp01(albedo.y);
        albedo.z = Mathf.Clamp01(albedo.z);

        specular.x = Mathf.Clamp01(specular.x);
        specular.y = Mathf.Clamp01(specular.y);
        specular.z = Mathf.Clamp01(specular.z);

        emission.x = Mathf.Clamp01(emission.x);
        emission.y = Mathf.Clamp01(emission.y);
        emission.z = Mathf.Clamp01(emission.z);
    }
}
