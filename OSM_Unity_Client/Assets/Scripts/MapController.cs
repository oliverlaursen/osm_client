using System.Diagnostics;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEditor.U2D.Animation;
using Unity.VisualScripting;
using System;
using System.Linq;

public class MapController : MonoBehaviour
{
    public Graph graph;

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
        var meshGenerator = GetComponent<MeshGenerator>();
        foreach (var way in ways.Values)
        {
            var positions = Array.ConvertAll(way, node => new Vector3(nodes[node][0], nodes[node][1], 0)).ToList();
            meshGenerator.AddLineStrip(positions, Color.white);
        }
        meshGenerator.UpdateMesh();
    }

    public void DrawPath(Dictionary<long, float[]> nodes, long[] path)
    {
        var lineRenderer = Camera.main.gameObject.GetComponent<GLLineRenderer>();
        var coords = Array.ConvertAll(path, node => new Vector3(nodes[node][0], nodes[node][1], 0)).ToList();
        lineRenderer.AddPath(coords);
    }

    void Start()
    {
        var graph = DeserializeGraph("Assets/Maps/denmark.json");
        this.graph = graph;
        DrawAllWays(graph.nodes, graph.ways);
    }
}
