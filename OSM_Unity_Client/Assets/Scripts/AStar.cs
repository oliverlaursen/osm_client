using System;
using System.Collections;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;
using UnityEngine.Assertions;

public class AStar : MonoBehaviour, IPathfindingAlgorithm
{
    public Graph graph;
    private FastPriorityQueue<PriorityQueueNode> openList;
    private HashSet<long> openSet;
    private HashSet<long> closedSet;
    private Dictionary<long, long> parent;
    private Dictionary<long, float> gScore;
    private Dictionary<long, float> fScore;
    private Dictionary<long, PriorityQueueNode> priorityQueueNodes;

    public AStar(Graph graph)
    {
        this.graph = graph;
    }

    public class PriorityQueueNode : FastPriorityQueueNode
    {
        public long Id { get; private set; }

        public PriorityQueueNode(long id)
        {
            Id = id;
        }
    }

    private void InitializeSearch(long start, long end)
    {
        //openList = new SimplePriorityQueue<long, float>();
        openList = new FastPriorityQueue<PriorityQueueNode>(graph.nodes.Count);
        openSet = new HashSet<long>();
        closedSet = new HashSet<long>();
        parent = new Dictionary<long, long>();
        gScore = new Dictionary<long, float>() { [start] = 0 };
        fScore = new Dictionary<long, float>() { [start] = HeuristicCostEstimate(start, end) };
        priorityQueueNodes = new Dictionary<long, PriorityQueueNode>();

        PriorityQueueNode startNode = new PriorityQueueNode(start);
        openList.Enqueue(startNode, fScore[start]);
        priorityQueueNodes[start] = startNode;
        openSet.Add(start);
    }

    public void FindShortestPath(long start, long end)
    {
        InitializeSearch(start, end);
        int nodesVisited = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (openList.Count > 0)
        {
            long current = DequeueAndUpdateSets(openList, openSet);
            if (ProcessCurrentNode(current, start, end, ref nodesVisited, stopwatch)) return;
            UpdateNeighbors(current, end);
        }
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end)
    {
        InitializeSearch(start, end);
        var lineRenderer = Camera.main.GetComponent<GLLineRenderer>(); // Ensure Camera has GLLineRenderer component
        int nodesVisited = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (openList.Count > 0)
        {
            long current = DequeueAndUpdateSets(openList, openSet);
            if (ProcessCurrentNode(current, start, end, ref nodesVisited, stopwatch))
            {
                lineRenderer.ClearDiscoveryPath();
                yield break;
            }
            UpdateNeighborsWithVisual(current, end, lineRenderer);
            yield return null;
        }
    }

    private long DequeueAndUpdateSets(FastPriorityQueue<PriorityQueueNode> openList, HashSet<long> openSet)
    {
        var current = openList.Dequeue().Id;
        openSet.Remove(current);
        return current;
    }

    private bool ProcessCurrentNode(long current, long start, long end, ref int nodesVisited, System.Diagnostics.Stopwatch stopwatch)
    {
        nodesVisited++;
        if (current == end)
        {
            stopwatch.Stop();
            DisplayPathFound(start, end, gScore[current], stopwatch.ElapsedMilliseconds, nodesVisited);
            return true;
        }
        return false;
    }

    private void DisplayPathFound(long start, long end, float cost, long elapsedMs, int nodesVisited)
    {
        MapController.DisplayStatistics(start, end, cost, elapsedMs, nodesVisited);
        GameObject.Find("Map").GetComponent<MapController>().DrawPath(graph.nodes, MapController.ReconstructPath(parent, start, end));
    }

    private void UpdateNeighbors(long current, long end)
    {
        foreach (var neighbor in graph.GetNeighbors(current))
        {
            if (closedSet.Contains(neighbor.node)) continue;
            TryEnqueueNeighbor(neighbor, current, end);
        }
    }

    private void UpdateNeighborsWithVisual(long current, long end, GLLineRenderer lineRenderer)
    {
        foreach (var neighbor in graph.GetNeighbors(current))
        {
            if (closedSet.Contains(neighbor.node)) continue;
            if (TryEnqueueNeighbor(neighbor, current, end))
            {
                var startCoord = graph.nodes[current];
                var endCoord = graph.nodes[neighbor.node];
                lineRenderer.AddDiscoveryPath(new List<Vector3> { new(startCoord.Item1[0], startCoord.Item1[1], 0), new(endCoord.Item1[0], endCoord.Item1[1], 0) });
            }
        }
    }

    private bool TryEnqueueNeighbor(Edge neighbor, long current, long end)
    {
        var tentativeGScore = gScore[current] + neighbor.cost;
        if (!gScore.ContainsKey(neighbor.node) || tentativeGScore < gScore[neighbor.node])
        {
            parent[neighbor.node] = current;
            gScore[neighbor.node] = tentativeGScore;
            fScore[neighbor.node] = tentativeGScore + HeuristicCostEstimate(neighbor.node, end);
            PriorityQueueNode neighborNode = new PriorityQueueNode(neighbor.node);
            if (!openSet.Contains(neighbor.node))
            {
                openList.Enqueue(neighborNode, fScore[neighbor.node]);
                priorityQueueNodes[neighbor.node] = neighborNode;
                openSet.Add(neighbor.node);
            }
            else
            {
                PriorityQueueNode nodeToUpdate = priorityQueueNodes[neighbor.node];
                openList.UpdatePriority(nodeToUpdate, fScore[neighbor.node]);
            }
            return true;
        }
        return false;
    }

    private float HeuristicCostEstimate(long start, long end)
    {
        var startCoords = graph.nodes[start];
        double startLat = startCoords.Item2[0]; // Convert to radians
        double startLon = startCoords.Item2[1]; // Convert to radians

        double startLat_radians = startLat * (Math.PI / 180);
        double startLon_radians = startLon * (Math.PI / 180);

        var endCoords = graph.nodes[end];
        double endLat = endCoords.Item2[0]; // Convert to radians
        double endLon = endCoords.Item2[1]; // Convert to radians

        double endLat_radians = endLat * (Math.PI / 180);
        double endLon_radians = endLon * (Math.PI / 180);

        double dLat = endLat_radians - startLat_radians;
        double dLon = endLon_radians - startLon_radians;

        double r = 6371000; //radius of the earth in meters

        double a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2)) +
                   (Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(startLat_radians) * Math.Cos(endLat_radians));
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        double dist = r * c;

        var floatDist = (float)dist;

        Assert.IsTrue(floatDist >= 0);

        return (float)dist;
    }
}