using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public Texture SkyboxTexture;
    public Texture BlockTexture;
    private Texture3D _voxelWorld;
    private RenderTexture _target;
    private Camera _camera;
    private int _maxSteps = 10;
    public int[] maxStepsSequence;
    private int _frameCount;
    private int _testStage;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Start()
    {
        _voxelWorld = new Texture3D(64, 64, 64, TextureFormat.RGBA32, false);
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                for (int z = 0; z < 64; z++)
                {
                    float c = Mathf.PerlinNoise(x, y) - z;
                    _voxelWorld.SetPixel(x, y, z, new Color(c, c, c, c));
                }
            }
        }
        Debug.Log("populated 3dtex");
    }

    private void SetShaderParameters()
    {
        RayTracingShader.SetInt("_MaxSteps", _maxSteps);
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetTexture(0, "_BlockTexture", BlockTexture);
        RayTracingShader.SetTexture(0, "_VoxelWorld", _voxelWorld);
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
        _frameCount++;
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();
        // Set the target and dispatch the compute shader
        RayTracingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        // Blit the result texture to the screen
        Graphics.Blit(_target, destination);
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();
            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }

    private void Update()
    {
                
    }

    private void FixedUpdate()
    {
        if (_testStage < maxStepsSequence.Length-1)
        {
            if (Time.realtimeSinceStartup - 5 >= _testStage)
            {
                _testStage++;
                Debug.Log("Test " + _testStage + ": " + _frameCount + " frames counted in one second at maxSteps="
                          +_maxSteps+" average frame render time: " + (1.0 / _frameCount).ToString("F4"));
                _maxSteps = maxStepsSequence[_testStage];
                _frameCount = 0;
            }
        }
    }
}