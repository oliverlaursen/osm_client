using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshGenerator : MonoBehaviour
{
    private Mesh lineMesh;
    private List<Vector3> vertices;
    private List<int> indices;

    void Awake()
    {
        // Initialize mesh and lists
        lineMesh = new Mesh();
        GetComponent<MeshFilter>().mesh = lineMesh;
        lineMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        vertices = new List<Vector3>();
        indices = new List<int>();
    }

    public void AddLine(Vector3 start, Vector3 end, Color color)
    {
        // Add vertices
        int startIndex = vertices.Count;
        vertices.Add(start);
        vertices.Add(end);

        // Add indices for the line
        indices.Add(startIndex);
        indices.Add(startIndex + 1);
    }

    public void AddLineStrip(List<Vector3> points, Color color)
    {
        // Add vertices
        int startIndex = vertices.Count;
        vertices.AddRange(points);

        // Add indices for the line strip
        for (int i = 0; i < points.Count - 1; i++)
        {
            indices.Add(startIndex + i);
            indices.Add(startIndex + i + 1);
        }

        // Update the mesh
    }

    public void UpdateMesh()
    {
        lineMesh.Clear();

        lineMesh.SetVertices(vertices);
        lineMesh.SetIndices(indices, MeshTopology.Lines, 0);
        lineMesh.RecalculateBounds();

        // Optional: Add colors or other attributes to the mesh
    }

    // Call this method to clear all lines
    public void ClearLines()
    {
        vertices.Clear();
        indices.Clear();
        UpdateMesh();
    }


}
