using UnityEngine;
using System.Collections.Generic;

public class GLLineRenderer : MonoBehaviour
{
    public Material lineMaterial; 
    private List<Vector3> linePoints = new List<Vector3>();
    private List<int> lineIndices = new List<int>();
    private Color lineColor = Color.white;

    void OnPostRender()
    {
        if (linePoints.Count < 2) return; // Nothing to draw

        GL.PushMatrix();
        lineMaterial.SetPass(0);
        GL.LoadProjectionMatrix(Camera.main.projectionMatrix);
        GL.Begin(GL.LINES);
        GL.Color(lineColor);

        for (int i = 0; i < lineIndices.Count; i += 2)
        {
            GL.Vertex(linePoints[lineIndices[i]]);
            GL.Vertex(linePoints[lineIndices[i + 1]]);
        }

        GL.End();
        GL.PopMatrix();
    }

    public void AddLine(Vector3 start, Vector3 end)
    {
        lineIndices.Add(linePoints.Count);
        linePoints.Add(start);

        lineIndices.Add(linePoints.Count);
        linePoints.Add(end);
    }

    public void ClearLines()
    {
        linePoints.Clear();
        lineIndices.Clear();
    }
}
