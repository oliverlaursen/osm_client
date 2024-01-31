using UnityEngine;
using System.Collections;

public class MapController : MonoBehaviour
{
    public TextAsset mapFile;
    // Use this for initialization
    void Start()
    {
        var preprocessed = PreProcess.PreProcessMap(mapFile);
        var coordinates = MapLoader.ProjectCoordinates(preprocessed.nodes);
        MapLoader.DrawRoads(coordinates, preprocessed.ways);
    }
}
