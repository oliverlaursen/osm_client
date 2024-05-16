using System;
using System.Collections;
using System.Collections.Generic;
using PlasticPipe.PlasticProtocol.Messages;
using UnityEngine;

public class BiAStar : IPathfindingAlgorithm
{
    private Graph graph;
    private AStar forwardAStar;
    private AStar backwardAStar;
    private long meetingNode;
    private float minDistance;

    public BiAStar(Graph graph)
    {
        this.graph = graph;
        forwardAStar = new AStar(graph);
        backwardAStar = new AStar(graph);
    }

    public void Initialize(long start, long end)
    {
        forwardAStar.InitializeSearch(start, end);
        backwardAStar.InitializeSearch(end, start);
        minDistance = float.PositiveInfinity;
        meetingNode = -1;
    }


    public PathResult FindShortestPath(long start, long end)
    {
        Initialize(start, end);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var beta = 0.5 * forwardAStar.heuristic.Calculate(start, end);

        while (forwardAStar.queue.Count > 0 && backwardAStar.queue.Count > 0)
        {
            var topf = forwardAStar.queue.First;
            var topr = backwardAStar.queue.First;

            if (forwardAStar.gScore[topf.Id] + backwardAStar.gScore[topr.Id] >= minDistance + beta)
            {
                Debug.Log("Start: " + start + " End: " + end);
                Debug.Log("topf.Priority: " + topf.Priority + " topr.Priority: " + topr.Priority + " minDistance: " + minDistance + " beta: " + beta);
                stopwatch.Stop();
                var allPrev = BiDijkstra.MergePrevious(forwardAStar.previous, backwardAStar.previous, meetingNode);
                var path = MapController.ReconstructPath(allPrev, start, end);
                var distance = BiDijkstra.ComputeDistance(path, graph);
                return new PathResult(start, end, distance, stopwatch.ElapsedMilliseconds, forwardAStar.nodesVisited + backwardAStar.nodesVisited, path);
            }

            // Process forward direction
            ProcessQueue(forwardAStar, backwardAStar, ref meetingNode, ref minDistance, true, end);

            // Process backward direction
            ProcessQueue(backwardAStar, forwardAStar, ref meetingNode, ref minDistance, false, start);
        }
        return null; // No path found
    }

    private void ProcessQueue(AStar activeAstar, AStar otherAstar, ref long meetingNode, ref float minDistance, bool isForward, long end, GLLineRenderer lineRenderer = null)
    {
        // Dequeue the closest node
        var currentNode = activeAstar.queue.Dequeue().Id;

        // Get neighbors based on direction
        var neighbors = isForward ? graph.graph[currentNode] : graph.bi_graph[currentNode];

        // Process all neighbors
        activeAstar.UpdateNeighbors(currentNode, end, neighbors);

        foreach (var edge in neighbors)
        {
            var neighbor = edge.node;
            if (otherAstar.gScore.ContainsKey(neighbor))
            {
                var potentialMinDistance = activeAstar.gScore[neighbor] + otherAstar.gScore[neighbor];
                if (potentialMinDistance < minDistance)
                {
                    minDistance = potentialMinDistance;
                    meetingNode = neighbor;
                }
            }
            if (lineRenderer != null)
            {
                var startCoord = graph.nodes[currentNode];
                var endCoord = graph.nodes[neighbor];
                lineRenderer.AddDiscoveryPath(new List<Vector3> { new Vector3(startCoord.Item1[0], startCoord.Item1[1], 0), new Vector3(endCoord.Item1[0], endCoord.Item1[1], 0) });
            }
        }
    }



    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        Initialize(start, end);
        var beta = 0.5 * forwardAStar.heuristic.Calculate(start,end);
        var lineRenderer = Camera.main.gameObject.GetComponent<GLLineRenderer>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();

        while (forwardAStar.queue.Count > 0 && backwardAStar.queue.Count > 0)
        {
            var topf = forwardAStar.queue.First;
            var topr = backwardAStar.queue.First;

            if (forwardAStar.gScore[topf.Id] + backwardAStar.gScore[topr.Id] >= minDistance + beta)
            {
                stopwatch.Stop();
                lineRenderer.ClearDiscoveryPath();
                var allPrev = BiDijkstra.MergePrevious(forwardAStar.previous, backwardAStar.previous, meetingNode);
                var path = MapController.ReconstructPath(allPrev, start, end);
                var distance = BiDijkstra.ComputeDistance(path, graph);
                var result = new PathResult(start, end, distance, stopwatch.ElapsedMilliseconds, forwardAStar.nodesVisited + backwardAStar.nodesVisited, path);
                result.DisplayAndDrawPath(graph);
                yield break;
            }
            // Process forward direction
            ProcessQueue(forwardAStar, backwardAStar, ref meetingNode, ref minDistance, true, end, lineRenderer);

            // Process backward direction
            ProcessQueue(backwardAStar, forwardAStar, ref meetingNode, ref minDistance, false, start, lineRenderer);

            // Drawing at intervals
            if (drawspeed == 0) yield return null;
            else if (stopwatch2.ElapsedTicks > drawspeed)
            {
                yield return null;
                stopwatch2.Restart();
            }

        }
        yield return null; // No path found
    }
}
