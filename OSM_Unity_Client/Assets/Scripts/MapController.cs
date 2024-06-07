using System.Diagnostics;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

using System.IO;
using System.Threading.Tasks;
using SimpleFileBrowser;

public class MapController : MonoBehaviour
{
    public Graph graph;
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
        if (loadingText != null)
        {
            loadingText.SetActive(true);
        }
        // Using stream and async deserialization
        using (var stream = File.OpenRead(mapFile))
        {
            var deserialized = await MessagePack.MessagePackSerializer.DeserializeAsync<GraphReadFormat>(stream);

            var n = deserialized.nodes.Length;
            var graph = new Edge[n][];
            var bi_graph = new Edge[n][];
            var nodes = new (float[], double[])[n];

            foreach (var node in deserialized.nodes)
            {
                int index = (int)node.id;  // Ensure this casting is valid based on your node ID generation logic.
                nodes[index] = (new float[] { node.x, node.y }, new double[] { node.lat, node.lon });
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

            if (loadingText != null)
            {
                loadingText.SetActive(false);
            }
            UnityEngine.Debug.Log("Deserialized " + mapFile + " with " + n + " nodes");
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
            var nodes = new (float[], double[])[n];

            foreach (var node in deserialized.nodes)
            {
                int index = (int)node.id;  // Ensure this casting is valid based on your node ID generation logic.
                nodes[index] = (new float[] { node.x, node.y }, new double[] { node.lat, node.lon });
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
        var stopwatch = new Stopwatch();
        stopwatch.Start();
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
        stopwatch.Stop();
        UnityEngine.Debug.Log("Drawing mesh " + stopwatch.ElapsedMilliseconds + " ms");
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
        Application.targetFrameRate = 144;
        QualitySettings.vSyncCount = 1;
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Graph Files", ".graph"));
        FileBrowser.SetDefaultFilter(".graph");
        FileBrowser.ShowLoadDialog(async (p) =>
        {
            var path = p.First();
            mapText.GetComponent<TMPro.TMP_Text>().text = path.Split('\\').Last().Split('/').Last();
            graph = await DeserializeGraphAsync(path, loadingText: loadingText);
            var height = GetHeight(graph.nodes);
            Camera.main.GetComponent<CameraControl>().maxOrthoSize = height / 2;
            Camera.main.orthographicSize = height / 2;
            Camera.main.GetComponent<CameraControl>().InitializeAlgorithms(graph);
            DrawAllEdges(graph.nodes, graph.graph);

        }, null, FileBrowser.PickMode.Files, false, Application.dataPath + "/Maps", "Select a graph", "Select");

    }
}
