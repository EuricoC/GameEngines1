using UnityEngine;

namespace Terrain
{
    public static class Noise
    {

        public enum NormalizeMode
        {
            Local,
            Global
        };
    
        public static float[,] GenerateNoiseMap(int seed, int mapWidth, int mapHeight, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
        {
            System.Random prng = new System.Random(seed);
        
            float[,] noiseMap = new float[mapWidth,mapHeight];
            Vector2[] octavesOffsets = new Vector2[octaves];

            float maxPHeight = 0;
            float amplitude = 1;
            float frequency = 1;

            for (int i = 0; i < octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + offset.x;
                float offsetY = prng.Next(-100000, 100000) - offset.y;
                octavesOffsets[i] = new Vector2(offsetX, offsetY);

                maxPHeight += amplitude;
                amplitude *= persistance;
            }

            if (scale <= 0)
            {
                scale = 0.0001f;
            }

            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;

            float halfWidth = mapWidth / 2f;
            float halfHeight = mapHeight / 2f;
        
            for (int i = 0; i < mapHeight; i++) //y
            {
                for (int j = 0; j < mapWidth; j++) //x
                {
                    amplitude = 1;
                    frequency = 1;
                    float noiseHeight = 0;
                
                    for (int k = 0; k < octaves; k++)
                    {
                        float sampleJ = (j-halfWidth + octavesOffsets[k].x)/scale * frequency;
                        float sampleI = (i-halfHeight + octavesOffsets[k].y)/scale * frequency;

                        float perlinValue = Mathf.PerlinNoise(sampleJ, sampleI) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistance;
                        frequency *= lacunarity;
                    }

                    if (noiseHeight > maxNoiseHeight)
                    {
                        maxNoiseHeight = noiseHeight;
                    }
                    else if (noiseHeight < minNoiseHeight)
                    {
                        minNoiseHeight = noiseHeight;
                    }
                
                    noiseMap[j, i] = noiseHeight;
                }   
            }

            for (int i = 0; i < mapHeight; i++)//y
            {
                for (int j = 0; j < mapWidth; j++)//x
                {
                    if (normalizeMode == NormalizeMode.Local)
                    {
                        noiseMap[j, i] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[j, i]);
                    }
                    else
                    {
                        float normalizedHeight = (noiseMap[j, i] + 1) / (maxPHeight);
                        noiseMap[j, i] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                    }
                
                }
            }

            return noiseMap;
        }
    }
}
