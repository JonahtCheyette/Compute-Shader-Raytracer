using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class SphereGenerationSettings : ScriptableObject {
    public event System.Action OnValuesUpdated;

    //the seed for sphere generation
    public int sphereSeed = 1000000;

    //the varaibles for sphere generation
    //the min and max radii
    public Vector2 sphereRadius = new Vector2(3f, 8f);
    //the number of spheres(not including spheres that got deleted to make it so that none of the spheres took up the other's space)
    public uint spheresMax = 100;
    //the circle we want the spheres to be in, centered at (0,0)
    public float spherePlacementRadius = 100f;
    //whether or not to populate the scene with emissive spheres
    public bool useEmissiveSpheres = true;
    public bool useSizeRange = false;
    [Range(0, 1)]
    public float emissiveChance = 0.05f;
    public Vector2 emissiveSizeRange = new Vector2(4, 7);

    [Range(0, 1)]
    public float metallicPercentage = 0.5f;
    [Range(0, 1)]
    public float nonMetalReflectiveness = 0.2f;

    public Vector2 metallicSmoothnessRange = Vector2.up;
    public Vector2 nonMetallicSmoothnessRange = Vector2.up;

    private void OnValidate() {
        ClampSmoothnessRanges();

        if (OnValuesUpdated != null) {
            OnValuesUpdated();
        }
    }

    private void ClampSmoothnessRanges() {
        //clamping the size of the spheres being generated
        sphereRadius.x = Mathf.Max(0, sphereRadius.x);
        sphereRadius.y = Mathf.Max(sphereRadius.x, sphereRadius.y);

        //smoothness range clamping
        //clamping them all to 0-1 range
        metallicSmoothnessRange.x = Mathf.Clamp01(metallicSmoothnessRange.x);
        metallicSmoothnessRange.y = Mathf.Clamp01(metallicSmoothnessRange.y);
        nonMetallicSmoothnessRange.x = Mathf.Clamp01(nonMetallicSmoothnessRange.x);
        nonMetallicSmoothnessRange.y = Mathf.Clamp01(nonMetallicSmoothnessRange.y);

        //making sure the x values don't go higher than the y values, and vice versa
        metallicSmoothnessRange.x = Mathf.Min(metallicSmoothnessRange.x, metallicSmoothnessRange.y);
        metallicSmoothnessRange.y = Mathf.Max(metallicSmoothnessRange.y, metallicSmoothnessRange.x);
        nonMetallicSmoothnessRange.x = Mathf.Min(nonMetallicSmoothnessRange.x, nonMetallicSmoothnessRange.y);
        nonMetallicSmoothnessRange.y = Mathf.Max(nonMetallicSmoothnessRange.y, nonMetallicSmoothnessRange.x);

        //emissive size range clamping
        emissiveSizeRange.x = Mathf.Clamp(emissiveSizeRange.x, sphereRadius.x, sphereRadius.y);
        emissiveSizeRange.y = Mathf.Clamp(emissiveSizeRange.y, sphereRadius.x, sphereRadius.y);
        //clamping the x value below the y value, and y value above the x value
        emissiveSizeRange.x = Mathf.Min(emissiveSizeRange.x, emissiveSizeRange.y);
        emissiveSizeRange.y = Mathf.Max(emissiveSizeRange.x, emissiveSizeRange.y);
    }

    public List<Sphere> GenerateSpheres() {
        Random.InitState(sphereSeed);
        List<Sphere> spheres = new List<Sphere>();
        // Add a number of random spheres
        for (int i = 0; i < spheresMax; i++) {
            Sphere sphere = new Sphere();

            // Radius and radius
            sphere.radius = sphereRadius.x + Random.value * (sphereRadius.y - sphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * spherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

            // Reject spheres that are intersecting others
            foreach (Sphere other in spheres) {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist) {
                    //evil >:)
                    goto SkipSphere;
                }
            }

            // Albedo and specular color
            Color color = Random.ColorHSV();

            bool metal = Random.value < metallicPercentage;
            sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * nonMetalReflectiveness;

            bool beatEmissiveOdds = Random.value < emissiveChance;
            //double ternary statement. Very evil.
            bool lightSource = useEmissiveSpheres ? useSizeRange ? sphere.radius >= emissiveSizeRange.x && sphere.radius <= emissiveSizeRange.y : beatEmissiveOdds : false;
            sphere.emission = lightSource ? Vector3.one : Vector3.zero;
            sphere.smoothness = metal ? Random.Range(metallicSmoothnessRange.x, metallicSmoothnessRange.y) : Random.Range(nonMetallicSmoothnessRange.x, nonMetallicSmoothnessRange.y);
            // Add the sphere to the list
            spheres.Add(sphere);
        SkipSphere:
            continue;
        }

        return spheres;
    }
}
