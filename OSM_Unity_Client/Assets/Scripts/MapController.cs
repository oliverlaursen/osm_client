using UnityEngine;
using System.Collections;

public class MapController : MonoBehaviour
{
    public TextAsset mapFile;
    // Use this for initialization
    void Start()
    {
        var preprocessed = PreProcess.PreProcessMap(mapFile);
        var mapLoader = gameObject.AddComponent<MapLoader>();
        var coordinates = mapLoader.ProjectCoordinates(preprocessed.nodes);
        MapLoader.DrawRoads(coordinates, preprocessed.ways);
    }
}
