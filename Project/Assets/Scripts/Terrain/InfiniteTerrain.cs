using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
    public class InfiniteTerrain : MonoBehaviour
    {
        private const float ChunkUpdateThreshold = 25f;
        private const float sqrChunkUpdateThreshold = ChunkUpdateThreshold * ChunkUpdateThreshold;
        private static float maxViewDst;
        public LODInfo[] detailLevel;
    
        public Transform viewer;
        public Material mapMaterial;

        public static Vector2 viewerPosition;
        private Vector2 viewerPositionOld;
    
        static MapGenerator mapGenerator;
        int chunkSize;
        int chunksVisibleInViewDst;

        private Dictionary<Vector2, TerrainChunk> terrainChunkDic = new Dictionary<Vector2, TerrainChunk>();
        static private List<TerrainChunk> terrainChunksVisiblePreviously = new List<TerrainChunk>();
        private void Start()
        {
            mapGenerator = FindObjectOfType<MapGenerator>();

            maxViewDst = detailLevel[detailLevel.Length-1].visibleDstTreshHold;
            chunkSize = mapGenerator.mapChunkSize - 1;
            chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
        
            UpdateVisibleChunks();
        }

        private void Update()
        {
            viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;

            if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrChunkUpdateThreshold)
            {
                viewerPositionOld = viewerPosition;
                UpdateVisibleChunks();
            }
        }

        void UpdateVisibleChunks()
        {
            for (int i = 0; i < terrainChunksVisiblePreviously.Count; i++)
            {
                terrainChunksVisiblePreviously[i].SetVisible(false);
            }
        
            terrainChunksVisiblePreviously.Clear();
        
            int currentChunkX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
            int currentChunkY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

            for (int yOffSet = -chunksVisibleInViewDst; yOffSet <= chunksVisibleInViewDst; yOffSet++)
            {
                for (int xOffSet = -chunksVisibleInViewDst; xOffSet <= chunksVisibleInViewDst; xOffSet++)
                {
                    Vector2 viewedChunkCoord = new Vector2(currentChunkX + xOffSet, currentChunkY + yOffSet);

                    if (terrainChunkDic.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDic[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        terrainChunkDic.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevel, transform, mapMaterial));
                    }
                }
            }
        }

        public class TerrainChunk
        {
            private GameObject meshObject;
            private Vector2 position;
            private Bounds bounds;

            private MeshRenderer meshRenderer;
            private MeshFilter meshFilter;
            private MeshCollider meshCollider;

            private LODInfo[] detailLevels;
            private LODMesh[] lodMeshes;
            private LODMesh collisionLODMesh;

            private MapData mapData;
            private bool mapDataReceived;

            private int previousLODIndex = -1;
        
            public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
            {
                this.detailLevels = detailLevels;
            
                position = coord * size;
                bounds = new Bounds(position, Vector2.one * size);
                Vector3 positionV3 = new Vector3(position.x, 0, position.y);

                meshObject = new GameObject("Terrain Chunk");
                meshRenderer = meshObject.AddComponent<MeshRenderer>();
                meshFilter = meshObject.AddComponent<MeshFilter>();
                meshCollider = meshObject.AddComponent<MeshCollider>();
            
                meshRenderer.material = material;
            
                meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
                meshObject.transform.parent = parent;
                meshObject.transform.localScale = Vector3.one*mapGenerator.terrainData.uniformScale;
                SetVisible(false);

                lodMeshes = new LODMesh[detailLevels.Length];

                for (int i = 0; i < detailLevels.Length; i++)
                {
                    lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
                    if (detailLevels[i].useForCollider)
                    {
                        collisionLODMesh = lodMeshes[i];
                    }
                }

                mapGenerator.RequestMapData(position, OnMapDataReceived);
            }

            void OnMapDataReceived(MapData mapData)
            {
                this.mapData = mapData;
                mapDataReceived = true;

                UpdateTerrainChunk();
            }

            public void UpdateTerrainChunk()
            {
                if (!mapDataReceived) return;
            
                float viewerDstFromNEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDstFromNEdge <= maxViewDst;

                if (visible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNEdge > detailLevels[i].visibleDstTreshHold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.received)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        
                        }
                        else if (!lodMesh.requested)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    if (lodIndex == 0)
                    {
                        if (collisionLODMesh.received)
                        {
                            meshCollider.sharedMesh = collisionLODMesh.mesh;
                        }
                        else if (!collisionLODMesh.requested)
                        {
                            collisionLODMesh.RequestMesh(mapData);
                        }
                    }
                    terrainChunksVisiblePreviously.Add(this);
                }
                
                SetVisible(visible);
            }

            public void SetVisible(bool visible)
            {
                meshObject.SetActive(visible);
            }

            public bool isVisible()
            {
                return meshObject.activeSelf;
            }
        }

        class LODMesh
        {
            public Mesh mesh;
            public bool requested;
            public bool received;
            private int lod;
            private System.Action updateCallBack;

            public LODMesh(int lod, System.Action updateCallBack)
            {
                this.lod = lod;
                this.updateCallBack = updateCallBack;
            }

            void OnMeshDataReceived(MeshData meshData)
            {
                mesh = meshData.CreateMesh();
                received = true;

                updateCallBack();
            }
        
            public void RequestMesh(MapData mapData)
            {
                requested = true;
                mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
            }
        }
    
        [System.Serializable]
        public struct LODInfo
        {
            public int lod;
            public float visibleDstTreshHold;
            public bool useForCollider;
        }
    }
}
