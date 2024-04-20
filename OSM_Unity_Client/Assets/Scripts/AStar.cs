using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;
using UnityEngine;
using UnityEngine.Assertions;

public class AStar : IPathfindingAlgorithm
{
    public Graph graph;
    private FastPriorityQueue<PriorityQueueNode> openList;
    private Dictionary<long, PriorityQueueNode> priorityQueueNodes;
    private HashSet<long> openSet;
    private HashSet<long> closedSet;
    private Dictionary<long, long> parent;
    private Dictionary<long, float> gScore;
    private Dictionary<long, float> fScore;
    private AStarHeuristic heuristic;
    private IEnumerable<Landmark> landmarks;

    public AStar(Graph graph, IEnumerable<Landmark> landmarks = null)
    {
        this.graph = graph;
        this.heuristic = new HaversineHeuristic(graph);
        this.landmarks = landmarks;
    }

    public void ChangeHeuristic(AStarHeuristic heuristic)
    {
        this.heuristic = heuristic;
    }

    public void InitializeSearch(long start, long end)
    {
        //openList = new SimplePriorityQueue<long, float>();
        openList = new FastPriorityQueue<PriorityQueueNode>(graph.nodes.Count);
        priorityQueueNodes = new Dictionary<long, PriorityQueueNode>();
        openSet = new HashSet<long>();
        closedSet = new HashSet<long>();
        parent = new Dictionary<long, long>();
        gScore = new Dictionary<long, float>() { [start] = 0 };
        fScore = new Dictionary<long, float>() { [start] = heuristic.Calculate(start, end) };

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
            UpdateLandmarks(nodesVisited, current, end);

        }
    }

    private void UpdateLandmarks(int nodesVisited, long start, long end, bool visual = false)
    {
        if (landmarks == null) return;
        if (nodesVisited % 100 == 0)
        {
            var bestLandmarks = Landmarks.FindBestLandmark(landmarks, start, end, 3);
            if (visual)
            {
                Landmarks.MarkLandmarks(landmarks.ToArray(), Color.blue);
                Landmarks.MarkLandmarks(bestLandmarks.Select(x => x.Item1).ToArray(), Color.yellow);
            }
            ChangeHeuristic(new MultLandmarkHeuristic(bestLandmarks.Select(x => new LandmarkHeuristic(x.Item1,x.Item2)).ToArray()));
        }
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        InitializeSearch(start, end);
        var lineRenderer = Camera.main.GetComponent<GLLineRenderer>(); // Ensure Camera has GLLineRenderer component
        int nodesVisited = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();

        while (openList.Count > 0)
        {
            long current = DequeueAndUpdateSets(openList, openSet);
            if (ProcessCurrentNode(current, start, end, ref nodesVisited, stopwatch))
            {
                lineRenderer.ClearDiscoveryPath();
                yield break;
            }
            UpdateNeighborsWithVisual(current, end, lineRenderer);
            UpdateLandmarks(nodesVisited, current, end, true);
            if (drawspeed == 0) yield return null;
            else if (stopwatch2.ElapsedTicks > drawspeed)
            {
                yield return null;
                stopwatch2.Restart();
            }
        }
    }

    public static long DequeueAndUpdateSets(FastPriorityQueue<PriorityQueueNode> openList, HashSet<long> openSet)
    {
        var current = openList.Dequeue().Id;
        openSet.Remove(current);
        return current;
    }

    public bool ProcessCurrentNode(long current, long start, long end, ref int nodesVisited, System.Diagnostics.Stopwatch stopwatch)
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
            fScore[neighbor.node] = tentativeGScore + heuristic.Calculate(neighbor.node, end);
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
}