using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PerlinVars
{
    public int numOctaves = 4;
    public float lacunarity = 2f;
    public float persistence = 0.5f;
}

[RequireComponent(typeof(MeshRenderer))]
public class Perlin_OOP : MonoBehaviour
{
    [Header("Inscribed")]
    public int textureSize = 1080;

    [Header("Inscribed/Dynamic")]
    public float noiseScale = 10;
    public Vector2 noiseOffset = Vector2.zero;
    public Vector2 offsetVal = new(1, 0);
    public PerlinVars perlinVars;

    Material _mat;
    Texture2D _tex;

    private void Start()
    {
        _mat = GetComponent<MeshRenderer>().material;

        InitTex();
        _mat.mainTexture = _tex;
    }

    void InitTex()
    {
        _tex = new(textureSize, textureSize, TextureFormat.RGBA32, false);

        UpdateTex();
    }

    private void Update()
    {
        noiseOffset += offsetVal * Time.deltaTime;
        UpdateTex();
    }

    void UpdateTex()
    {
        // Generate the array to fill
        Color[] pixels = new Color[textureSize * textureSize];

        // Precalculate some math
        float noiseMult = noiseScale / (float)textureSize;
        int ndx = 0;
        float minusHalf = -textureSize * 0.5f;
        for (int v = 0; v < textureSize; v++)
        {
            for (int h = 0; h < textureSize; h++)
            {
                float x = ((minusHalf + h) * noiseMult) + noiseOffset.x;
                float y = ((minusHalf + v) * noiseMult) + noiseOffset.y;

                float u = PerlinOctaves(perlinVars, x, y);

                pixels[ndx++] = new(u, u, u, 1);
            }
        }

        _tex.SetPixels(pixels);
        _tex.Apply(false);
    }

    float PerlinOctaves(PerlinVars perlinVars, float x, float y)
    {
        float u = 0, lacu = 1, pers = 1;

        for (int octave = 0; octave < perlinVars.numOctaves; octave++)
        {
            if (octave != 0)
            {
                lacu *= perlinVars.lacunarity;
                pers *= perlinVars.persistence;
            }

            u += (Mathf.PerlinNoise(x * lacu, y * lacu) - 0.5f) * pers;
        }

        return u + 0.5f;
    }
}
