using System.Diagnostics;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public class MapController : MonoBehaviour
{
    private long[] ReconstructPath(Dictionary<long, long> previous, long start, long end)
            {
                var path = new List<long>();
                var current = end;
                while (current != start)
                {
                    path.Add(current);
                    current = previous[current];
                }
                path.Add(start);
                path.Reverse();
                return path.ToArray();
            }
    public (float, long[]) Dijkstra(Graph graph, long start, long end)
    {
        var nodes = graph.nodes;
        var edges = graph.graph;
        var visited = new HashSet<long>();
        var distances = new Dictionary<long, float>();
        var previous = new Dictionary<long, long>();
        var queue = new SortedSet<(float, long)>();

        foreach (var node in nodes.Keys)
        {
            distances[node] = float.MaxValue;
            previous[node] = -1;
        }

        distances[start] = 0;
        queue.Add((0, start));

        while (queue.Count > 0)
        {
            var (distance, node) = queue.Min;
            queue.Remove(queue.Min);
            if (visited.Contains(node))
            {
                continue;
            }
            visited.Add(node);

            if (node == end)
            {
                return (distance, ReconstructPath(previous, start, end));
            }

            foreach (var edge in edges[node])
            {
                var neighbor = edge.node;
                var cost = edge.cost;
                var newDistance = distance + cost;
                if (newDistance < distances[neighbor])
                {
                    distances[neighbor] = newDistance;
                    previous[neighbor] = node;
                    queue.Add((newDistance, neighbor));
                }
            }
        }

        return (float.MaxValue, new long[0]);
    }

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

    public void DrawPath(Dictionary<long, float[]> nodes, long[] path)
    {
        for (int j = 0; j < path.Length - 1; j++)
        {
            var node1 = nodes[path[j]];
            var node2 = nodes[path[j + 1]];
            var node1Vector = new Vector3(node1[0], node1[1], 0);
            var node2Vector = new Vector3(node2[0], node2[1], 0);
            UnityEngine.Debug.DrawLine(node1Vector, node2Vector, Color.green, 10000f);
        }
    }

    void Start()
    {
        var graph = DeserializeGraph("Assets/Maps/andorra.json");
        DrawAllWays(graph.nodes, graph.ways);
        var start = 51361816;
        var end = 1922638424;
        var (distance, path) = Dijkstra(graph, start, end);
        UnityEngine.Debug.Log("Distance: " + distance);
        DrawPath(graph.nodes, path);
    }
}
