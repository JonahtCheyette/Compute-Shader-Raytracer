using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour {
    bool useAntiAliasing = true;
    bool useDiffuseLighting = true;
    bool moveSpheres = false;

    //[Range(0, 7)]
    int maxNumReflections = 2;

    //the seed for sphere generation
    public int sphereSeed;

    //the varaibles for sphere generation
    //the min and max radii
    public Vector2 sphereRadius = new Vector2(3.0f, 8.0f);
    //the number of spheres(not including spheres that got deleted to make it so that none of the spheres took up the other's space)
    public uint spheresMax = 100;
    //the circle we want the spheres to be in, centered at (0,0)
    public float spherePlacementRadius = 100.0f;
    //how far along their "bob cycle" each sphere is
    private List<float> bobCycle = new List<float>();

    private ComputeBuffer sphereBuffer;

    //the shader to use
    public ComputeShader rayTracingShader;

    //the scene's light
    public Light directionalLight;

    //the texture that will be filled by the shader, then blited to the screen
    private RenderTexture target;

    //the camera the script is attatched to
    private Camera _camera;

    //the skybox texture
    public Texture skyboxTexture;
    
    //the current amount of sampled spots in each pixel (gets reset with each time the camera moves
    private uint currentSample = 0;
    private Material addMaterial;
    
    //the spheres
    List<Sphere> spheres = new List<Sphere>();

    //our buffer to hold the results with of our shaders with high precision
    private RenderTexture converged;

    private void OnEnable() {
        currentSample = 0;
        SetUpScene();
    }

    private void OnDisable() {
        if (sphereBuffer != null) {
            sphereBuffer.Release();
        }
    }

    private void SetUpScene() {
        Random.InitState(sphereSeed);
        spheres = new List<Sphere>();
        bobCycle = new List<float>();
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

            //getting random points in a sine wave and adding them to the positions with the values being mapped to the range [0, 1]
            bobCycle.Add(Random.Range(0, 6.283185f));
            if (moveSpheres) {
                sphere.position.y += (Mathf.Sin(bobCycle[bobCycle.Count - 1]) + 1) / 2f;
            }

            // Albedo and specular color
            Color color = Random.ColorHSV();
            //float spec = Random.value;
            bool metal = Random.value < 0.5f;
            sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.4f;
            bool lightSource = Random.value * (sphere.radius - sphereRadius.x) / (sphereRadius.y - sphereRadius.x) < 0.05f;

            sphere.emission = lightSource ? Vector3.one * 4f : Vector3.zero;
            sphere.smoothness = Random.value;
            // Add the sphere to the list
            spheres.Add(sphere);
        SkipSphere:
            continue;
        }
        // Assign to compute buffer
        sphereBuffer = new ComputeBuffer(spheres.Count, 56);
        sphereBuffer.SetData(spheres);
    }

    private void Awake() {
        _camera = GetComponent<Camera>();
    }

    private void Update() {
        //resetting the current amount of samples
        if (transform.hasChanged || directionalLight.transform.hasChanged) {
            currentSample = 0;
            transform.hasChanged = false;
            directionalLight.transform.hasChanged = false;
        }
        if (moveSpheres) {
            if (spheres.Count == bobCycle.Count) {
                for (int i = 0; i < bobCycle.Count; i++) {
                    bobCycle[i] += 0.005f;
                    Sphere s = spheres[i];
                    s.position.y = spheres[i].radius + (Mathf.Sin(bobCycle[i]) + 1) / 2f;
                    spheres[i] = s;
                }
            }

            // Assign to compute buffer
            if (sphereBuffer != null) {
                sphereBuffer.Release();
            }
            sphereBuffer = new ComputeBuffer(spheres.Count, 56);
            sphereBuffer.SetData(spheres);
        }
    }

    private void SetShaderParameters() {
        Vector3 l = directionalLight.transform.forward;
        rayTracingShader.SetVector("directionalLight", new Vector4(l.x, l.y, l.z, directionalLight.intensity));
        if (useAntiAliasing) {
            rayTracingShader.SetVector("pixelOffset", new Vector2(Random.value, Random.value));
        } else {
            rayTracingShader.SetVector("pixelOffset", new Vector2(0.5f, 0.5f));
        }
        rayTracingShader.SetBool("useDiffuseLighting", useDiffuseLighting);
        rayTracingShader.SetInt("maxNumReflections", maxNumReflections + 1);
        rayTracingShader.SetTexture(0, "SkyboxTexture", skyboxTexture);
        rayTracingShader.SetMatrix("cameraToWorld", _camera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("cameraInverseProjection", _camera.projectionMatrix.inverse);
        rayTracingShader.SetBuffer(0, "spheres", sphereBuffer);
        rayTracingShader.SetFloat("seed", Random.value);
    }

    //called every frame by unity, which automatically passes in what's already rendered as the source and the camera's target (in most cases, the screen) as the destination
    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        SetShaderParameters();
        Render(destination);
    }

    private void Render(RenderTexture destination) {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        rayTracingShader.SetTexture(0, "result", target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 4.0f);
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        // Blit the result texture to the screen
        if (addMaterial == null) {
            addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        }
        if (useAntiAliasing) {
            addMaterial.SetFloat("Sample", currentSample);
            //this function essentially copies the source texture to the destination texture. If a material is passed in, it applies that material's shader to the texture, using the source as the materials' _MainTex variable
            Graphics.Blit(target, converged, addMaterial);
            Graphics.Blit(converged, destination);
            currentSample++;
        } else {
            Graphics.Blit(target, destination);
        }
    }

    private void InitRenderTexture() {
        if (target == null || target.width != Screen.width || target.height != Screen.height) {
            // Release render texture if we already have one
            if (target != null) {
                target.Release();
            }
            //reset the current amount of sampled vectors for each pixel
            currentSample = 0;

            // Get a render target for Ray Tracing
            target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }

        if (converged == null || converged.width != Screen.width || converged.height != Screen.height) {
            // Release render texture if we already have one
            if (converged != null) {
                converged.Release();
            }
            //reset the current amount of sampled vectors for each pixel
            currentSample = 0;

            converged = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            converged.enableRandomWrite = true;
            converged.Create();
        }
    }
}

public struct Sphere {
    public Vector3 position;
    public float radius;
    public Vector3 albedo;
    public Vector3 specular;
    public float smoothness;
    public Vector3 emission;
}