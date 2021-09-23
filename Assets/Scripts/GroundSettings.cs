using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class GroundSettings : ScriptableObject {
    public event System.Action OnValuesUpdated;

    public Vector3 albedo = Vector3.one * 0.5f;
    public Vector3 specular = Vector3.one * 0.05f;
    public float smoothness = 0.2f;
    public Vector3 emission = Vector3.zero;

    private void OnValidate() {
        if (OnValuesUpdated != null) {
            OnValuesUpdated();
        }
    }
}
