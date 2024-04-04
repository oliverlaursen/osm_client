using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class GLLineRenderer : MonoBehaviour
{
    public Material lineMaterial;
    public (List<List<Vector3>>, Color) path { get; set; } = (new(), Color.red);
    public (List<List<Vector3>>, Color) discoveryPath { get; set; } = (new(), Color.green);

    public void OnPostRender()
    {
        if (!lineMaterial)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }

        GL.PushMatrix();
        lineMaterial.SetPass(0);
        GL.LoadProjectionMatrix(Camera.main.projectionMatrix);

        GL.Begin(GL.LINES);
        DrawLines(discoveryPath.Item1, discoveryPath.Item2);
        DrawLines(path.Item1, path.Item2);
        GL.End();

        GL.PopMatrix();
    }

    public void DrawLines(List<List<Vector3>> lines, Color color)
    {
        GL.Color(color);
        foreach (var line in lines)
        {
            for (int i = 0; i < line.Count - 1; i++)
            {
                GL.Vertex(line[i]);
                GL.Vertex(line[i + 1]);
            }
        }
    }

    public void AddPath(List<Vector3> path)
    {
        this.path.Item1.Add(path);
    }

    public void AddDiscoveryPath(List<Vector3> path)
    {
        this.discoveryPath.Item1.Add(path);
    }

    public void ClearPath()
    {
        path = (new(), Color.red);
    }

    public void ClearDiscoveryPath()
    {
        discoveryPath = (new(), Color.red);
    }
}