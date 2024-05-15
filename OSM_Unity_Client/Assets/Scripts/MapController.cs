using System.Diagnostics;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

using System.IO;
using System.Threading.Tasks;
using System.Collections;

public class MapController : MonoBehaviour
{
    public Graph graph;
    public string mapFileName = "andorra.graph";
    public GameObject mapText;
    public GameObject loadingText;

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
        var startText = GameObject.Find("StartField");
        var endText = GameObject.Find("EndField");
        var distanceText = GameObject.Find("Distance");
        var nodesVisitedText = GameObject.Find("NodesVisited");
        var timeText = GameObject.Find("TimeText");

        ChangeTextFieldHelper(startText, start.ToString());
        ChangeTextFieldHelper(endText, end.ToString());
        ChangeTextHelper(distanceText, "Distance: " + distance);
        ChangeTextHelper(timeText, "Time (ms): " + timeElapsed);
        ChangeTextHelper(nodesVisitedText, "Nodes visited: " + nodesVisited);

    }

    public static void ChangeTextHelper(GameObject gameObject, string text)
    {
        gameObject.GetComponent<TMPro.TMP_Text>().text = text;
    }

    public static void ChangeTextFieldHelper(GameObject gameObject, string text)
    {
        gameObject.GetComponent<TMPro.TMP_InputField>().text = text;
    }

    public static async Task<Graph> DeserializeGraphAsync(string mapFile, GameObject loadingText = null)
    {
        if (loadingText != null){
            loadingText.SetActive(true);
        }
        // Using stream and async deserialization
        using (var stream = File.OpenRead(mapFile))
        {
            var deserialized = await MessagePack.MessagePackSerializer.DeserializeAsync<GraphReadFormat>(stream);

            var n = deserialized.nodes.Length;
            var graph = new Edge[n][];
            var bi_graph = new Edge[n][];
            var nodes = new (float[],double[])[n];

            foreach (var node in deserialized.nodes)
            {
                int index = (int)node.id;  // Ensure this casting is valid based on your node ID generation logic.
                nodes[index] = (new float[] {node.x, node.y}, new double[] {node.lat, node.lon});
                graph[index] = node.neighbours;
                bi_graph[index] = node.bi_neighbours;
            }

            var full_graph = new Graph
            {
                nodes = nodes,
                graph = graph,
                bi_graph = bi_graph,
                landmarks = deserialized.landmarks
            };

            if (loadingText != null){
                loadingText.SetActive(false);
            }

            return full_graph;
        }
    }

    public static Graph DeserializeGraph(string mapFile)
    {
        // Using stream and async deserialization
        using (var stream = File.OpenRead(mapFile))
        {
            var deserialized = MessagePack.MessagePackSerializer.Deserialize<GraphReadFormat>(stream);

            var n = deserialized.nodes.Length;
            var graph = new Edge[n][];
            var bi_graph = new Edge[n][];
            var nodes = new (float[],double[])[n];

            foreach (var node in deserialized.nodes)
            {
                int index = (int)node.id;  // Ensure this casting is valid based on your node ID generation logic.
                nodes[index] = (new float[] {node.x, node.y}, new double[] {node.lat, node.lon});
                graph[index] = node.neighbours;
                bi_graph[index] = node.bi_neighbours;
            }

            var full_graph = new Graph
            {
                nodes = nodes,
                graph = graph,
                bi_graph = bi_graph,
                landmarks = deserialized.landmarks
            };
            return full_graph;
        }
    }


    public void DrawAllEdges((float[], double[])[] nodes, Edge[][] graph)
    {
        var meshGenerator = GetComponent<MeshGenerator>();
        foreach (var element in graph.Select((Value, Index) => new { Value, Index }))
        {
            var startNode = element.Index;
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

    public void DrawPath((float[], double[])[] nodes, long[] path)
    {
        var lineRenderer = Camera.main.gameObject.GetComponent<GLLineRenderer>();
        var coords = Array.ConvertAll(path, node => new Vector3(nodes[node].Item1[0], nodes[node].Item1[1], 0)).ToList();
        lineRenderer.AddPath(coords);
    }

    public float GetHeight((float[], double[])[] nodes)
    {
        var min = float.MaxValue;
        var max = float.MinValue;
        foreach (var node in nodes)
        {
            min = Mathf.Min(min, node.Item1[1]);
            max = Mathf.Max(max, node.Item1[1]);
        }
        return max - min;
    }

    async void Start()
    {
        mapText.GetComponent<TMPro.TMP_Text>().text = mapFileName;
        var time = new Stopwatch();
        time.Start();
        var graph = await DeserializeGraphAsync("Assets/Maps/" + mapFileName, loadingText: loadingText);
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
