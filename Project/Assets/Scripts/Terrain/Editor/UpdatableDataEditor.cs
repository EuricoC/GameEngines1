using Terrain.TerrainData;
using UnityEditor;
using UnityEngine;

namespace Terrain.Editor
{
    [CustomEditor(typeof(UpdatableData), true)]
    public class UpdatableDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            UpdatableData data = (UpdatableData) target;

            if (GUILayout.Button("Update"))
            {
                data.NotifyOfUpdatedValues();
            }
        }
    }
}
