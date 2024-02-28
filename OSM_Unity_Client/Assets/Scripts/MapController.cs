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

    public (float, long[]) AStar(Graph graph, long start, long end)
    {
        UnityEngine.Debug.Log("A*");
        var meshGenerator = GetComponent<MeshGenerator>();
        var nodes = graph.nodes;

        // For sorting by fScore
        var openList = new SortedSet<(float, long)>();
        // For quick existence checks
        var openSet = new HashSet<long>();
        var closedList = new HashSet<long>();

        //initialize g and f scores 
        //g are the costs from the start node to the current node, 
        //f are the costs from the start node to the current node + the heuristic to the end node
        var gScores = new Dictionary<long, float> { [start] = 0 };
        var fScores = new Dictionary<long, float> { [start] = HeuristicCostEstimate(start, end) };
        var cameFrom = new Dictionary<long, long>();

        int nodesVisited = 0;

        //add the start node to the open list
        //openList.Add((fScores[start], start));
        //testOpenList.Add(start, fScores[start]);
        openList.Add((fScores[start], start));
        openSet.Add(start);
        cameFrom[start] = -1;

        //while the open list is not empty
        while (openList.Count > 0)
        {
            nodesVisited++;
            //get the node with the lowest f score
            var current = openList.Min;

            //remove it from the open list
            openList.Remove(current);
            openSet.Remove(current.Item2);

            // for each neighbor of the current node
            foreach (var neighbor in graph.GetNeighbors(current.Item2))
            {
                var g = GameObject.Find("Map").GetComponent<MapController>().graph;
                
                Dictionary<long, long> lol = new(){[neighbor.node] = current.Item2};
                var path = ReconstructPath(lol, current.Item2, neighbor.node);
                var lineRenderer = Camera.main.gameObject.GetComponent<GLLineRenderer>();
                lineRenderer.ClearPath();
                GameObject.Find("Map").GetComponent<MapController>().DrawPath(g.nodes, path);



                //cost from start through current node to the neighbor
                var tentativeGScore = gScores[current.Item2] + neighbor.cost;

                // if neighbor is the end node, return the path
                if (neighbor.node == end)
                {
                    cameFrom[neighbor.node] = current.Item2;
                    UnityEngine.Debug.Log("nodes visited " + nodesVisited);
                    return (tentativeGScore, ReconstructPath(cameFrom, start, end));
                }
                else
                {
                    gScores[neighbor.node] = tentativeGScore;

                    //calculate the f score
                    float neighborFScore = gScores[neighbor.node] + HeuristicCostEstimate(neighbor.node, end);

                    if (openSet.Contains(neighbor.node) && fScores[neighbor.node] < neighborFScore)
                    {
                        continue;
                    }
                    if (closedList.Contains(neighbor.node) && fScores.GetValueOrDefault(neighbor.node, float.MaxValue) < neighborFScore)
                    {
                        continue;
                    }
                    else
                    {
                        cameFrom[neighbor.node] = current.Item2;
                        fScores[neighbor.node] = neighborFScore;
                        openList.Add((neighborFScore, neighbor.node));
                        openSet.Add(neighbor.node);
                    }
                }
            }
            //add it to the closed list
            closedList.Add(current.Item2);
        }
        return (float.MaxValue, new long[0]);
    }

    private float HeuristicCostEstimate(long start, long end)
    {
        // Implement your heuristic here. This could be Manhattan, Euclidean, etc.
        // For now, let's assume it's Euclidean distance.
        var startCoords = graph.nodes[start];
        var endCoords = graph.nodes[end];
        return Mathf.Sqrt(Mathf.Pow(endCoords[0] - startCoords[0], 2) + Mathf.Pow(endCoords[1] - startCoords[1], 2));
    }

    public (float, long[]) Dijkstra(Graph graph, long start, long end)
    {
        UnityEngine.Debug.Log("Dijkstra");
        var nodes = graph.nodes;
        var edges = graph.graph;
        var visited = new HashSet<long>();
        var distances = new Dictionary<long, float>();
        var previous = new Dictionary<long, long>();
        var queue = new SortedSet<(float, long)>();

        int nodesVisited = 0;

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

            nodesVisited++;

            if (node == end)
            {
                UnityEngine.Debug.Log("nodes visited " + nodesVisited);
                return (distance, ReconstructPath(previous, start, end));
            }

            foreach (var edge in edges[node])
            {
                var firstCoord = new Vector3(nodes[node][0], nodes[node][1], 0);
                var secondCoord = new Vector3(nodes[edge.node][0], nodes[edge.node][1], 0);

                UnityEngine.Debug.DrawLine(firstCoord, secondCoord, Color.green, 0.0f);

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

    public void DrawGreenLine(Vector3 node1, Vector3 node2)
{
    GL.Begin(GL.LINES);
    GL.Color(Color.green);

    GL.Vertex(node1);
    GL.Vertex(node2);

    GL.End();
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
        foreach (var node in deserialized.nodes)
        {
            nodes[node.id] = new float[] { node.x, node.y };
            var edges = new List<Edge>();
            for (int i = 0; i < node.neighbours.Length; i++)
            {
                edges.Add(new Edge { node = node.neighbours[i].Item1, cost = node.neighbours[i].Item2 });
            }
            graph[node.id] = edges.ToArray();
        }
        return new Graph { nodes = nodes, graph = graph };
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
