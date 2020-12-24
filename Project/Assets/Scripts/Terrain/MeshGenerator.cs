using UnityEngine;
using Valve.VR.InteractionSystem;

namespace Terrain
{
  public static class MeshGenerator
  {
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
      AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
    
      int meshTriangleSimplicity = (levelOfDetail == 0)?1:levelOfDetail * 2;
    
      int borderedSize = heightMap.GetLength(0);
      int meshSize = borderedSize - 2*meshTriangleSimplicity;
      int meshTriangleUnSimplicity = borderedSize - 2;
    
      float topLeftx = (meshTriangleUnSimplicity - 1) / -2f;
      float topLeftz = (meshTriangleUnSimplicity - 1) / 2f;
    
      int verticesPLine = (meshSize - 1) / meshTriangleSimplicity + 1;

      MeshData meshData = new MeshData(verticesPLine);

      int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
      int meshVertexIndex = 0;
      int boarderVertexIndex = -1;

      for (int i = 0; i < borderedSize; i += meshTriangleSimplicity) //y
      {
        for (int j = 0; j < borderedSize; j += meshTriangleSimplicity) //x
        {
          bool isBorderVertex = i == 0 || i == borderedSize - 1 || j == 0 || j == borderedSize - 1;

          if (isBorderVertex)
          {
            vertexIndicesMap[j, i] = boarderVertexIndex;
            boarderVertexIndex--;
          }
          else
          {
            vertexIndicesMap[j, i] = meshVertexIndex;
            meshVertexIndex++;
          }
        }
      }

      for (int i = 0; i < borderedSize; i+= meshTriangleSimplicity)//y
      {
        for (int j = 0; j < borderedSize; j+= meshTriangleSimplicity)//x
        {
          int vertexIndex = vertexIndicesMap[j, i];
          Vector2 percent = new Vector2((j - meshTriangleSimplicity)/ (float) meshSize, (i - meshTriangleSimplicity) / (float) meshSize);
          float height = heightCurve.Evaluate(heightMap[j, i]) * heightMultiplier;
          Vector3 vertexPosition = new Vector3(topLeftx + percent.x * meshTriangleUnSimplicity,height, topLeftz - percent.y * meshTriangleUnSimplicity);
        
          meshData.AddVertex(vertexPosition,percent,vertexIndex);

          if (j < borderedSize - 1 && i < borderedSize - 1)
          {
            int a = vertexIndicesMap[j, i];
            int b = vertexIndicesMap[j + meshTriangleSimplicity, i];
            int c = vertexIndicesMap[j, i + meshTriangleSimplicity];
            int d = vertexIndicesMap[j + meshTriangleSimplicity, i + meshTriangleSimplicity];
            meshData.AddTriangle(a,d, c);
            meshData.AddTriangle(d,a,b);
          }
        
          vertexIndex++;
        
        }
      }
    
      meshData.BakedNormals();
    
      return meshData;
    }
  }

  public class MeshData
  {
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    private Vector3[] bakedNormals;

    private Vector3[] borderVertices;
    private int[] borderTriangles;

    int triangleIndex;
    private int borderTriangleIndex;

    public MeshData(int verticesPLine)
    {
      vertices = new Vector3[verticesPLine * verticesPLine];
      triangles = new int[(verticesPLine - 1) * (verticesPLine - 1) * 6];
      uvs = new Vector2[verticesPLine*verticesPLine];

      borderVertices = new Vector3[verticesPLine * 4 + 4];
      borderTriangles = new int[24 * verticesPLine];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
      if (vertexIndex < 0)
      {
        borderVertices[-vertexIndex - 1] = vertexPosition;
      }
      else
      {
        vertices[vertexIndex] = vertexPosition;
        uvs[vertexIndex] = uv;
      }
    }

    public void AddTriangle(int a, int b, int c)
    {
      if (a<0 || b<0 || c<0)
      {
        borderTriangles[borderTriangleIndex] = a;
        borderTriangles[borderTriangleIndex+1] = b;
        borderTriangles[borderTriangleIndex+2] = c;
        borderTriangleIndex += 3;
      }
      else
      {
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c;
        triangleIndex += 3;
      }
    }

    Vector3[] CalculateNormals()
    {
      Vector3[] vertexNormals = new Vector3[vertices.Length];
      int triangleCnt = triangles.Length / 3;

      for (int i = 0; i < triangleCnt; i++)
      {
        int normalTriangleIndex = i * 3;
        int vertexIndexA = triangles[normalTriangleIndex];
        int vertexIndexB = triangles[normalTriangleIndex + 1];
        int vertexIndexC = triangles[normalTriangleIndex + 2];

        Vector3 triangleNormal = SurfaceNormalFIndices(vertexIndexA, vertexIndexB, vertexIndexC);
        vertexNormals[vertexIndexA] += triangleNormal;
        vertexNormals[vertexIndexB] += triangleNormal;
        vertexNormals[vertexIndexC] += triangleNormal;
      }
    
      int borderTriangleCnt = borderTriangles.Length / 3;
      for (int i = 0; i < borderTriangleCnt; i++)
      {
        int normalTriangleIndex = i * 3;
        int vertexIndexA = borderTriangles[normalTriangleIndex];
        int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
        int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

        Vector3 triangleNormal = SurfaceNormalFIndices(vertexIndexA, vertexIndexB, vertexIndexC);
        if (vertexIndexA>=0)
        {
          vertexNormals[vertexIndexA] += triangleNormal;
        }

        if (vertexIndexB>=0)
        {
          vertexNormals[vertexIndexB] += triangleNormal;
        }
      
        if (vertexIndexC>=0)
        {
          vertexNormals[vertexIndexC] += triangleNormal;
        }
      }

      for (int i = 0; i < vertexNormals.Length; i++)
      {
        vertexNormals[i].Normalize();
      }

      return vertexNormals;
    }

    Vector3 SurfaceNormalFIndices(int indexA, int indexB, int indexC)
    {
      Vector3 pointA = (indexA<0)?borderVertices[-indexA-1] : vertices[indexA];
      Vector3 pointB = (indexB<0)?borderVertices[-indexB-1] : vertices[indexB];
      Vector3 pointC = (indexC<0)?borderVertices[-indexC-1] : vertices[indexC];

      Vector3 sideAB = pointB - pointA;
      Vector3 sideAC = pointC - pointA;

      return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void BakedNormals()
    {
      bakedNormals = CalculateNormals();
    }
    public Mesh CreateMesh()
    {
      Mesh mesh = new Mesh();
      mesh.vertices = vertices;
      mesh.triangles = triangles;
      mesh.uv = uvs;
      mesh.normals = bakedNormals;

      return mesh;
    }
  }
}