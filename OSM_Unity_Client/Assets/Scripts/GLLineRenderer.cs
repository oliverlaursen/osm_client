using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class ColoredLine
{
    public List<Vector3> Points { get; set; }
    public Color Color { get; set; }
}

public class GLLineRenderer : MonoBehaviour
{
    public Material lineMaterial;
    public (List<List<Vector3>>, Color) map_lines { get; set; } = (new(), Color.white);
    public (List<List<Vector3>>, Color) circles1 { get; set; } = (new(), Color.green);
    public (List<List<Vector3>>, Color) circles2 { get; set; } = (new(), new Color(1, 0.64f, 0, 1));
    public (List<List<Vector3>>, Color) path { get; set; } = (new(), Color.red);

    void OnPostRender()
    {
        if (!lineMaterial)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }

        GL.PushMatrix();
        lineMaterial.SetPass(0);
        GL.LoadProjectionMatrix(Camera.main.projectionMatrix);

        DrawLines(map_lines.Item1, map_lines.Item2);
        DrawLines(circles1.Item1, circles1.Item2);
        DrawLines(circles2.Item1, circles2.Item2);
        DrawLines(path.Item1, path.Item2);

        GL.PopMatrix();
    }

    public void DrawLines(List<List<Vector3>> lines, Color color)
    {
        foreach (var line in lines)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);

            for (int i = 0; i < line.Count - 1; i++)
            {
                GL.Vertex(line[i]);
                GL.Vertex(line[i + 1]);
            }
            GL.End();
        }
    }


    public List<Vector3> GetCircle(Vector3 center, float radius, int resolution = 100)
    {
        List<Vector3> points = new List<Vector3>();

        for (int i = 0; i < resolution; i++)
        {
            float angle = i * 2 * Mathf.PI / resolution;
            points.Add(new Vector3(center.x + radius * Mathf.Cos(angle), center.y + radius * Mathf.Sin(angle), center.z));
        }
        return points;
    }

    public void AddCircle1(Vector3 center)
    {
        var circle = GetCircle(center, 2.5f);
        circles1.Item1.Add(circle);
    }

    public void AddCircle2(Vector3 center)
    {
        var circle = GetCircle(center, 2.5f);
        circles2.Item1.Add(circle);
    }

    public void AddPath(List<Vector3> path)
    {
        this.path.Item1.Add(path);
    }

    public void AddMapLine(List<Vector3> path)
    {
        map_lines.Item1.Add(path);
    }

    public void ClearPath()
    {
        path = (new(), Color.red);
    }

    public void ClearCircle1()
    {
        circles1 = (new(), Color.green);
    }

    public void ClearCircle2()
    {
        circles2 = (new(), new Color(1, 0.64f, 0, 1));
    }
}