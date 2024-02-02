using System.Diagnostics;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public TextAsset mapFile;
    public ComputeShader projectionShader;
    // Use this for initialization
    void Start()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        var preprocessed = PreProcess.PreProcessMap(mapFile);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Preprocessing took " + stopwatch.ElapsedMilliseconds + " ms");
        stopwatch.Reset();

        var mapLoader = gameObject.AddComponent<MapLoader>();
        stopwatch.Start();
        var coordinates = mapLoader.ProjectCoordinates(preprocessed.nodes, projectionShader);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Projection took " + stopwatch.ElapsedMilliseconds + " ms");
        stopwatch.Reset();

        stopwatch.Start();
        MapLoader.DrawRoads(coordinates, preprocessed.ways);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Drawing roads took " + stopwatch.ElapsedMilliseconds + " ms");
    }
}
