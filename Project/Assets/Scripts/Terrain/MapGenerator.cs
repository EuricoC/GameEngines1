using System;
using System.Collections.Generic;
using System.Threading;
using Terrain.TerrainData;
using UnityEngine;

namespace Terrain
{
    public class MapGenerator : MonoBehaviour
    {
        public enum DrawMode{NoiseMap, Mesh}
        public DrawMode drawMode;

        public TerrainData.TerrainData terrainData;
        public NoiseData noiseData;
        public TextureData textureData;
    
        public Material terrainMaterial;
    
        [Range(0,6)]
        public int levelOfDetailPreview;

        public bool autoUpdate;


        private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQ = new Queue<MapThreadInfo<MapData>>();
        private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQ = new Queue<MapThreadInfo<MeshData>>();

        void OnValuesUpdated()
        {
            if (!Application.isPlaying)
            {
                DrawMapInEditor();
            }
        }
    
        void OnTextureValuesUpdated() 
        {
            textureData.ApplyToMaterial(terrainMaterial);
        }

        public int mapChunkSize = 239;


        public void DrawMapInEditor()
        {
            MapData mapData = GenerateMapData(Vector2.zero);
        
            MapDisplay display = FindObjectOfType<MapDisplay>();
            if (drawMode == DrawMode.NoiseMap)
            {
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
            }
            else if (drawMode == DrawMode.Mesh)
            {
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, levelOfDetailPreview));
            }
        }
    
    
        public void RequestMapData(Vector2 centre, Action<MapData> callBack)
        {
            ThreadStart threadStart = delegate
            {
                MapDataThread(centre, callBack);
            };
        
            new Thread(threadStart).Start();
        }

        void MapDataThread(Vector2 centre, Action<MapData> callBack)
        {
            MapData mapData = GenerateMapData(centre);
            lock (mapDataThreadInfoQ)
            {
                mapDataThreadInfoQ.Enqueue(new MapThreadInfo<MapData>(callBack, mapData));
            }
        }
    
        public void RequestMeshData(MapData mapData,int lod, Action<MeshData> callback)
        {
            ThreadStart threadStart = delegate
            {
                MeshDataThread(mapData, lod, callback);
            };
        
            new Thread(threadStart).Start();
        }
    
        void MeshDataThread(MapData mapData, int lod, Action<MeshData> callBack)
        {
            MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod);
            lock (meshDataThreadInfoQ)
            {
                meshDataThreadInfoQ.Enqueue(new MapThreadInfo<MeshData>(callBack, meshData));
            }
        }

        private void Update()
        {
            if(mapDataThreadInfoQ.Count > 0)
            {
                for (int i = 0; i < mapDataThreadInfoQ.Count; i++)
                {
                    MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQ.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }
        
            if(meshDataThreadInfoQ.Count > 0)
            {
                for (int i = 0; i < meshDataThreadInfoQ.Count; i++)
                {
                    MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQ.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }
        }

        MapData GenerateMapData(Vector2 centre)
        {
            float[,] noiseMap = Noise.GenerateNoiseMap(noiseData.seed, mapChunkSize + 2, mapChunkSize + 2, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity,centre + noiseData.offset, noiseData.normalizeMode);

            return new MapData(noiseMap);
        }

        void OnValidate()
        {
            if (terrainData != null)
            {
                terrainData.OnValuesUpdated -= OnValuesUpdated;
                terrainData.OnValuesUpdated += OnValuesUpdated;
            }
            if (noiseData != null)
            {
                noiseData.OnValuesUpdated -= OnValuesUpdated;
                noiseData.OnValuesUpdated += OnValuesUpdated;
            }
            if (textureData != null) 
            {
                textureData.OnValuesUpdated -= OnTextureValuesUpdated;
                textureData.OnValuesUpdated += OnTextureValuesUpdated;
            }
        }

        struct MapThreadInfo<T>
        {
            public readonly Action<T> callback;
            public readonly T parameter;

            public MapThreadInfo(Action<T> callback, T parameter)
            {
                this.callback = callback;
                this.parameter = parameter;
            }
        }
    }

    public struct MapData
    {
        public readonly float[,] heightMap;

        public MapData(float[,] heightMap)
        {
            this.heightMap = heightMap;
        }
    }
}