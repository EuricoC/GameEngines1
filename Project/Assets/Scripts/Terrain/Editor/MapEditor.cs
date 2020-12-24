using UnityEditor;
using UnityEngine;

namespace Terrain.Editor
{
  [CustomEditor(typeof(MapGenerator))]
  public class MapEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      MapGenerator mapGen = (MapGenerator)target;

      if (DrawDefaultInspector())
      {
        if (mapGen.autoUpdate)
        {
          mapGen.DrawMapInEditor();
        }
      }

      if (GUILayout.Button("Generate"))
      {
        mapGen.DrawMapInEditor();
      }
    }
  }
}
