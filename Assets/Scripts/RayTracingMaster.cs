using System.Collections.Generic;
using UnityEngine;

[ImageEffectAllowedInSceneView]
[ExecuteAlways]
public class RayTracingMaster : MonoBehaviour {
    [Range(0, 7)]
    public int maxNumReflections = 2;

    [Range(0,3)]
    public float skyboxLighting = 1;

    public GroundSettings groundSettings;

    public SphereGenerationSettings sphereGeneration;
    private ComputeBuffer sphereBuffer;

    //the shader to use
    public ComputeShader rayTracingShader;

    //the texture that will be filled by the shader, then blited to the screen
    private RenderTexture target;

    //the skybox texture
    public Texture skyboxTexture;
    
    //the current amount of sampled spots in each pixel (gets reset with each time the camera moves
    private uint currentSample = 0;
    private Material addMaterial;

    //our buffer to hold the results with of our shaders with high precision
    private RenderTexture converged;

    //used to prevent erroneous error messages when switching from playmode to editor mode
    private bool wasPlayingLastFrame;

    private void Start() {
        ResetScene();
    }

    private void Update() {
        ResetSampleCount();
        wasPlayingLastFrame = true;
    }

    private void OnValidate() {
        SetAutoUpdateUp();
        ResetScene();
    }

    private void SetAutoUpdateUp() {
        if (groundSettings != null) {
            groundSettings.OnValuesUpdated -= ResetScene;
            groundSettings.OnValuesUpdated += ResetScene;
        }
        if (sphereGeneration != null) {
            sphereGeneration.OnValuesUpdated -= ResetScene;
            sphereGeneration.OnValuesUpdated += ResetScene;
        }
    }

    private void OnDisable() {
        ReleaseSphereBuffer();
    }

    //called every frame by unity, which automatically passes in what's already rendered as the source and the camera's target (in most cases, the screen) as the destination
    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (!Application.isPlaying) {
            ResetSampleCount();
        }
        SetDynamicShaderParameters();
        Render(destination);
    }

    private void ResetScene() {
        if (sphereGeneration != null && groundSettings != null) {
            currentSample = 0;
            SetUpScene();
            SetStaticShaderVariables();
        } else {
            if (sphereGeneration == null && groundSettings == null) {
                print("No Sphere Generation Settings or Ground Settings found. Please attatch both to the RayTracingMaster script attatched to the camera in the editor");
            } else if (sphereGeneration == null) {
                print("No Sphere Generation Settings found. Please attatch one to the RayTracingMaster script attatched to the camera in the editor");
            } else {
                print("No Ground Settings found. Please attatch one to the RayTracingMaster script attatched to the camera in the editor");
            }
        }
    }

    private void ResetSampleCount() {
        //resetting the current amount of samples
        if (transform.hasChanged) {
            currentSample = 0;
            transform.hasChanged = false;
        }
    }

    private void ReleaseSphereBuffer() {
        if (sphereBuffer != null) {
            sphereBuffer.Release();
        }
    }

    private void SetUpScene() {
        List<Sphere> spheres = sphereGeneration.GenerateSpheres();
        // Assign to compute buffer
        if(sphereBuffer != null && sphereBuffer.IsValid()){
            if (sphereBuffer.count != spheres.Count) {
                sphereBuffer.Dispose();
            }
        }
        if (sphereBuffer == null || !sphereBuffer.IsValid()) {
            sphereBuffer = new ComputeBuffer(spheres.Count, 56);
        }
        sphereBuffer.SetData(spheres);
    }

    private void SetStaticShaderVariables() {
        if (rayTracingShader != null) {
            rayTracingShader.SetInt("maxNumReflections", maxNumReflections + 1);
            //note, in the compute shader the texture is actually called samplerSkyboxTexture, because samplers are weird
            rayTracingShader.SetTexture(0, "SkyboxTexture", skyboxTexture);
            rayTracingShader.SetBuffer(0, "spheres", sphereBuffer);
            rayTracingShader.SetVector("groundAlbedo", groundSettings.albedo);
            rayTracingShader.SetVector("groundSpecular", groundSettings.specular);
            rayTracingShader.SetFloat("groundSmoothness", groundSettings.smoothness);
            rayTracingShader.SetVector("groundEmission", groundSettings.emission);
        }
    }

    private void SetDynamicShaderParameters() {
        rayTracingShader.SetVector("pixelOffset", new Vector2(Random.value, Random.value));
        rayTracingShader.SetMatrix("cameraToWorld", Camera.current.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("cameraInverseProjection", Camera.current.projectionMatrix.inverse);
        rayTracingShader.SetFloat("seed", Random.value);
        rayTracingShader.SetFloat("skyboxLighting", skyboxLighting);
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
        addMaterial.SetFloat("Sample", currentSample);
        //this function essentially copies the source texture to the destination texture. If a material is passed in, it applies that material's shader to the texture, using the source as the materials' _MainTex variable
        Graphics.Blit(target, converged, addMaterial);
        Graphics.Blit(converged, destination);
        currentSample++;
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
    public Vector3 specular; //the tint of the sphere's reflection
    public float smoothness;
    public Vector3 emission; // whether the sphere emits light
}