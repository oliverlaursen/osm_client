using System.Diagnostics;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

using System.IO;

public class MapController : MonoBehaviour
{
    public Graph graph;
    public string mapFileName = "andorra.graph";
    public GameObject mapText;

    public static long[] ReconstructPath(Dictionary<long, long> previous, long start, long end)
    {
        var path = new List<long>();
        var current = end;

        // Check if there is a path from start to end
        if (!previous.ContainsKey(current))
        {
            // Path not found, return an empty array or handle the error accordingly
            UnityEngine.Debug.Log("the end is not reachable from the start node");
            return new long[0];
        }
        // Reconstruct the path
        while (current != start)
        {
            path.Add(current);
            // Ensure the key exists before accessing it
            if (!previous.ContainsKey(current))
            {
                // Handle the error or return an incomplete path
                return new long[0];
            }
            current = previous[current];
        }
        path.Add(start);
        path.Reverse();
        return path.ToArray();
    }

    public static void DisplayStatistics(long start, long end, float distance, long timeElapsed, int nodesVisited)
    {
        var startText = GameObject.Find("Start");
        var endText = GameObject.Find("End");
        var distanceText = GameObject.Find("Distance");
        var nodesVisitedText = GameObject.Find("NodesVisited");
        var timeText = GameObject.Find("TimeText");

        ChangeTextHelper(startText, "Start: " + start);
        ChangeTextHelper(endText, "End: " + end);
        ChangeTextHelper(distanceText, "Distance: " + distance);
        ChangeTextHelper(timeText, "Time (ms): " + timeElapsed);
        ChangeTextHelper(nodesVisitedText, "Nodes visited: " + nodesVisited);

    }

    public static void ChangeTextHelper(GameObject gameObject, string text)
    {
        gameObject.GetComponent<TMPro.TMP_Text>().text = text;
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
        var nodes = new Dictionary<long, (float[],double[])>();
        var graph = new Dictionary<long, Edge[]>();
        var bi_graph = new Dictionary<long, Edge[]>();
        foreach (var node in deserialized.nodes)
        {
            nodes[node.id] = (new float[] {node.x, node.y}, new double[] {node.lat, node.lon});
            var edges = new List<Edge>();
            for (int i = 0; i < node.neighbours.Length; i++)
            {
                edges.Add(new Edge { node = node.neighbours[i].Item1, cost = node.neighbours[i].Item2 });
            }
            graph[node.id] = edges.ToArray();

            var bi_edges = new List<Edge>();
            for(int i=0; i<node.bi_neighbours.Length; i++)
            {
                bi_edges.Add(new Edge { node = node.bi_neighbours[i].Item1, cost = node.bi_neighbours[i].Item2 });
            }
            bi_graph[node.id] = bi_edges.ToArray();
        }
        var full_graph = new Graph { nodes = nodes, graph = graph, bi_graph = bi_graph, landmarks = deserialized.landmarks };
        return full_graph;
    }

    public void DrawAllEdges(Dictionary<long, (float[],double[])> nodes, Dictionary<long, Edge[]> graph)
    {
        var meshGenerator = GetComponent<MeshGenerator>();
        foreach (var element in graph)
        {
            var startNode = element.Key;
            var startPos = nodes[startNode];
            var startCoord = new Vector3(startPos.Item1[0], startPos.Item1[1], 0);
            var edges = element.Value;
            var amountOfEdges = edges.Count();
            for (int i = 0; i < amountOfEdges; i++)
            {
                var endNode = edges[i].node;
                var endPos = nodes[endNode];
                var endCoord = new Vector3(endPos.Item1[0], endPos.Item1[1], 0);
                meshGenerator.AddLine(startCoord, endCoord, Color.white);
            }
        }
        meshGenerator.UpdateMesh();
    }

    public void DrawPath(Dictionary<long, (float[],double[])> nodes, long[] path)
    {
        var lineRenderer = Camera.main.gameObject.GetComponent<GLLineRenderer>();
        var coords = Array.ConvertAll(path, node => new Vector3(nodes[node].Item1[0], nodes[node].Item1[1], 0)).ToList();
        lineRenderer.AddPath(coords);
    }

    public float GetHeight(Dictionary<long, (float[],double[])> nodes)
    {
        var min = float.MaxValue;
        var max = float.MinValue;
        foreach (var node in nodes.Values)
        {
            min = Mathf.Min(min, node.Item1[1]);
            max = Mathf.Max(max, node.Item1[1]);
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
        Camera.main.GetComponent<CameraControl>().InitializeAlgorithms(graph);
        DrawAllEdges(graph.nodes, graph.graph);
        UnityEngine.Debug.Log("Draw time: " + time.ElapsedMilliseconds + "ms");
        time.Stop();
    }
}
