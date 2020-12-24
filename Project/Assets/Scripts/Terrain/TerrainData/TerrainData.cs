using UnityEngine;

namespace Terrain.TerrainData
{
    [CreateAssetMenu()]
    public class TerrainData : UpdatableData
    {
        public float uniformScale = 2.5f;
    
        public float meshHeightMultiplier;
        public AnimationCurve meshHeightCurve;
    }
}
