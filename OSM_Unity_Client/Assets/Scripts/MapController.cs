using System.Diagnostics;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public class MapController : MonoBehaviour
{
    public Graph DeserializeGraph(string filename)
    {
        string json = System.IO.File.ReadAllText(filename);
        var graph = JsonConvert.DeserializeObject<Graph>(json);
        return graph;
    }

    public void DrawAllWays(Dictionary<long, float[]> nodes, Dictionary<long, long[]> ways)
    {
        foreach (var way in ways.Values)
        {
            for (int j = 0; j < way.Length - 1; j++)
            {
                var node1 = nodes[way[j]];
                var node2 = nodes[way[j + 1]];
                var node1Vector = new Vector3(node1[0], node1[1], 0);
                var node2Vector = new Vector3(node2[0], node2[1], 0);
                UnityEngine.Debug.DrawLine(node1Vector, node2Vector, Color.red, 10000f);
            }
        }
    }

    void Start()
    {
        var graph = DeserializeGraph("Assets/Maps/andorra.json");
        DrawAllWays(graph.nodes, graph.ways);
    }
}
