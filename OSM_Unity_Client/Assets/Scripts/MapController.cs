using System.Diagnostics;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public TextAsset mapFile;
    private GLLineRenderer lineRenderer;

    void Start()
    {
        // Get GLLinerenderer from camera
        lineRenderer = Camera.main.GetComponent<GLLineRenderer>();

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        var preprocessed = PreProcess.PreProcessMap(mapFile);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Preprocessing took " + stopwatch.ElapsedMilliseconds + " ms");
        stopwatch.Reset();

        var mapLoader = gameObject.AddComponent<MapLoader>();
        stopwatch.Start();
        var coordinates = mapLoader.ProjectCoordinates(preprocessed.nodes);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Projection took " + stopwatch.ElapsedMilliseconds + " ms");
        stopwatch.Reset();

        stopwatch.Start();
        MapLoader.DrawRoads(coordinates, preprocessed.ways, lineRenderer);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Drawing roads took " + stopwatch.ElapsedMilliseconds + " ms");
    }
}
