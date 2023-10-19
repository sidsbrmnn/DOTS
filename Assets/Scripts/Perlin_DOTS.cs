using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Jobs;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using Unity.Burst;

[BurstCompile]
public struct PerlinImageJob : IJobParallelFor
{
    [WriteOnly] public NativeArray<Color32> pixels;
    [ReadOnly] public int textureSize;
    [ReadOnly] public float2 noiseOffset;
    [ReadOnly] public float noiseMult;
    [ReadOnly] public float minusHalf;
    [ReadOnly] public float numOctaves, lacunarity, persistence;
    [ReadOnly] public bool vignette;
    [ReadOnly] public bool colorize;
    [ReadOnly] public NativeArray<Color32> clut;

    public void Execute(int ndx)
    {
        int h = ndx % textureSize;
        int v = ndx / textureSize;

        float2 loc;
        loc.x = ((minusHalf + h) * noiseMult) + noiseOffset[0];
        loc.y = ((minusHalf + v) * noiseMult) + noiseOffset[1];

        float u = 0, lacu = 1, pers = 1;
        for (int i = 0; i < numOctaves; i++)
        {
            if (i != 0)
            {
                lacu *= lacunarity;
                pers *= persistence;
            }

            u += noise.cnoise(loc * lacu) * pers;
        }

        u = (u + 1) * 0.5f;

        // Apply the vignette after moving u to the range =[0..1]
        if (vignette)
        {
            loc.x = h / (float)textureSize;
            loc.y = v / (float)textureSize;

            loc.x = (loc.x - 0.5f) * 3f;
            loc.y = (loc.y - 0.5f) * 3f;

            loc.x = 1 - (loc.x * loc.x);
            loc.y = 1 - (loc.y * loc.y);

            u *= (loc.x + loc.y) * 0.5f;
        }

        if (u < 0) u = 0;
        if (u > 1) u = 1;

        // Apply to the pixels
        byte b = (byte)(255 * u);
        pixels[ndx] = colorize ? clut[b] : new(b, b, b, 255);
    }
}

[RequireComponent(typeof(MeshRenderer))]
public class Perlin_DOTS : MonoBehaviour
{
    [Header("Inscribed")]
    public int textureSize = 1080;
    public Gradient colorGradient;
    public bool regenCLUT = false;

    [Header("Inscribed/Dynamic")]
    public float noiseScale = 10;
    public Vector2 noiseOffset = Vector2.zero;
    public Vector2 offsetVal = new(1, 0);
    public PerlinVars perlinVars;
    public bool vignette = true;
    public bool colorize = true;

    Material _mat;
    Texture2D _tex;
    Color32[] clut; // Color LookUp Table

    // Start is called before the first frame update
    void Start()
    {
        _mat = GetComponent<MeshRenderer>().material;
        _tex = new(textureSize, textureSize, TextureFormat.RGBA32, false);
        _mat.mainTexture = _tex;

        GenerateCLUT();
    }

    void GenerateCLUT()
    {
        // Generate the Color LookUp Table
        clut = new Color32[256];
        float step = 1f / 255f;

        Color32 color;
        float u = 0;
        for (int i = 0; i < clut.Length; i++)
        {
            color = colorGradient.Evaluate(u);
            clut[i] = color;
            u += step;
        }
        regenCLUT = false;
    }

    // Update is called once per frame
    void Update()
    {
        // Regenerate CLUT when regenCLUT is checked
        if (regenCLUT) GenerateCLUT();

        noiseOffset += offsetVal * Time.deltaTime;
        UpdateTex();
    }

    void UpdateTex()
    {
        // Prepare the job, including precalculating some math
        PerlinImageJob job = new()
        {
            pixels = _tex.GetRawTextureData<Color32>(),
            textureSize = textureSize,
            noiseOffset = noiseOffset,
            noiseMult = noiseScale / textureSize,
            minusHalf = -textureSize * 0.5f,
            numOctaves = perlinVars.numOctaves,
            lacunarity = perlinVars.lacunarity,
            persistence = perlinVars.persistence,
            vignette = vignette,
            colorize = colorize,
            clut = new(clut, Allocator.TempJob)
        };

        // Schedule the job
        JobHandle handle = job.Schedule(job.pixels.Length, 64);

        // Wait for the job to complete
        handle.Complete();

        // CLean up the job.clut memory allocation
        job.clut.Dispose();

        _tex.Apply(false);
    }
}
