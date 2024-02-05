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
    public List<ColoredLine> Lines { get; set; } = new List<ColoredLine>();

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
        GL.Begin(GL.LINES);

        foreach (var line in Lines)
        {
            if (line.Points.Count < 2) continue; // Nothing to draw for this line

            GL.Color(line.Color);
            for (int i = 0; i < line.Points.Count - 1; i++)
            {
                GL.Vertex(line.Points[i]);
                GL.Vertex(line.Points[i + 1]);
            }
        }

        GL.End();
        GL.PopMatrix();
    }

    public void AddLine(List<Vector3> points, Color color)
    {
        Lines.Add(new ColoredLine { Points = points, Color = color });
    }

    public void AddCircle(Vector3 center, float radius, Color color, int resolution = 100)
    {
        List<Vector3> points = new List<Vector3>();

        for (int i = 0; i < resolution; i++)
        {
            float angle = i * 2 * Mathf.PI / resolution;
            points.Add(new Vector3(center.x + radius * Mathf.Cos(angle), center.y + radius * Mathf.Sin(angle), center.z));
        }
        AddLine(points, color);
    }

    public void AddCircle1(Vector3 center)
    {
        AddCircle(center, 2.5f, Color.green);
    }

    public void AddCircle2(Vector3 center)
    {
        AddCircle(center, 2.5f, new Color(1, 0.64f, 0, 1));
    }

    public void Clear()
    {
        Lines.Clear();
    }

    public void ClearPath(){
        Lines = Lines.Where(line => line.Color != Color.red).ToList();
    }

    public void ClearCircle1(){
        Lines = Lines.Where(line => line.Color != Color.green).ToList();
    }

    public void ClearCircle2(){
        Lines = Lines.Where(line => line.Color != new Color(1, 0.64f, 0, 1)).ToList();
    }
}