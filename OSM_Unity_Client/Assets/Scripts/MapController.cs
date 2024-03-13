using System.Diagnostics;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEditor.U2D.Animation;
using Unity.VisualScripting;
using System;
using System.Linq;

using System.IO;
using System.Globalization;
using System.Threading.Tasks;

public class MapController : MonoBehaviour
{
    public Graph graph;
    public string mapFileName = "andorra.graph";
    public GameObject mapText;

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


    public static Graph DeserializeGraph(string mapFile)
    {
        /**
           Format:
           nodeId x y neighbour cost neighbour cost
           long float float long int long int
        */        
        var input = File.ReadAllBytes(mapFile);
        var deserialized = MessagePack.MessagePackSerializer.Deserialize<GraphReadFormat>(input);
        var nodes = new Dictionary<long, float[]>();
        var graph = new Dictionary<long, Edge[]>();
        var bi_graph = new Dictionary<long, Edge[]>();
        foreach (var node in deserialized.nodes)
        {
            nodes[node.id] = new float[] {node.x, node.y, node.lat, node.lon};
            var edges = new List<Edge>();
            for (int i = 0; i < node.neighbours.Length; i++)
            {
                edges.Add(new Edge {node = node.neighbours[i].Item1, cost = node.neighbours[i].Item2});
            }
            graph[node.id] = edges.ToArray();

            var bi_edges = new List<Edge>();
            for(int i=0; i<node.bi_neighbours.Length; i++)
            {
                bi_edges.Add(new Edge { node = node.bi_neighbours[i].Item1, cost = node.bi_neighbours[i].Item2 });
            }
        }
        var full_graph = new Graph { nodes = nodes, graph = graph, bi_graph = bi_graph };
        return full_graph;
    }

    public void DrawAllEdges(Dictionary<long, float[]> nodes, Dictionary<long, Edge[]> graph)
    {
        var meshGenerator = GetComponent<MeshGenerator>();
        foreach (var element in graph)
        {
            var startNode = element.Key;
            var startPos = nodes[startNode];
            var startCoord = new Vector3(startPos[0], startPos[1], 0);
            var edges = element.Value;
            var amountOfEdges = edges.Count();
            for (int i = 0; i < amountOfEdges; i++)
            {
                var endNode = edges[i].node;
                var endPos = nodes[endNode];
                var endCoord = new Vector3(endPos[0], endPos[1], 0);
                meshGenerator.AddLine(startCoord, endCoord, Color.white);
            }
        }
        meshGenerator.UpdateMesh();
    }

    public void DrawPath(Dictionary<long, float[]> nodes, long[] path)
    {
        var lineRenderer = Camera.main.gameObject.GetComponent<GLLineRenderer>();
        var coords = Array.ConvertAll(path, node => new Vector3(nodes[node][0], nodes[node][1], 0)).ToList();
        lineRenderer.AddPath(coords);
    }

    public float GetHeight(Dictionary<long, float[]> nodes)
    {
        var min = float.MaxValue;
        var max = float.MinValue;
        foreach (var node in nodes.Values)
        {
            min = Mathf.Min(min, node[1]);
            max = Mathf.Max(max, node[1]);
        }
        return max - min;
    }

    void Start()
    {
        mapText.GetComponent<TMPro.TMP_Text>().text = mapFileName;
        var time = new Stopwatch();
        time.Start();
        var graph = DeserializeGraph("Assets/Maps/" + mapFileName);
        UnityEngine.Debug.Log("Deserialization time: " + time.ElapsedMilliseconds + "ms");
        time.Reset();
        time.Start();
        var height = GetHeight(graph.nodes);
        Camera.main.GetComponent<CameraControl>().maxOrthoSize = height / 2;
        Camera.main.orthographicSize = height / 2;
        this.graph = graph;
        DrawAllEdges(graph.nodes, graph.graph);
        UnityEngine.Debug.Log("Draw time: " + time.ElapsedMilliseconds + "ms");
        time.Stop();
    }
}
