using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiDijkstra : MonoBehaviour, IPathfindingAlgorithm
{
    public Graph graph;
    private Dijkstra forwardDijkstra;
    private Dijkstra backwardDijkstra;
    private long meetingNode;
    private float minDistance;
    private long startNode, endNode;

    public BiDijkstra(Graph graph)
    {
        this.graph = graph;
        forwardDijkstra = new Dijkstra(graph);
        backwardDijkstra = new Dijkstra(graph);
    }

    private void Initialize(long start, long end, Graph graph)
    {
        forwardDijkstra.InitializeSearch(start, graph);
        backwardDijkstra.InitializeSearch(end, graph);
        minDistance = float.PositiveInfinity;
        meetingNode = -1;
    }

    public PathResult FindShortestPath(long start, long end)
    {
        startNode = start;
        endNode = end;
        Initialize(start, end, graph);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Initialize both searches
        forwardDijkstra.InitializeSearch(start, graph);
        backwardDijkstra.InitializeSearch(end, graph);

        while (forwardDijkstra.queue.Count > 0 && backwardDijkstra.queue.Count > 0)
        {
            // Process forward direction
            if (ProcessQueue(forwardDijkstra, backwardDijkstra, ref meetingNode, ref minDistance, true))
            {
                stopwatch.Stop();
                var path = MergePrevious(forwardDijkstra.previous, backwardDijkstra.previous, meetingNode);

                return new PathResult(start, end, minDistance, stopwatch.ElapsedMilliseconds, forwardDijkstra.nodesVisited + backwardDijkstra.nodesVisited, MapController.ReconstructPath(path, start, end));
            }

            // Process backward direction
            if (ProcessQueue(backwardDijkstra, forwardDijkstra, ref meetingNode, ref minDistance, false))
            {
                stopwatch.Stop();
                var path = MergePrevious(forwardDijkstra.previous, backwardDijkstra.previous, meetingNode);

                return new PathResult(start, end, minDistance, stopwatch.ElapsedMilliseconds, forwardDijkstra.nodesVisited + backwardDijkstra.nodesVisited, MapController.ReconstructPath(path, start, end));
            }
        }

        stopwatch.Stop();
        return new PathResult(start, end, -1, stopwatch.ElapsedMilliseconds, forwardDijkstra.nodesVisited + backwardDijkstra.nodesVisited, new long[] { });
    }

    private bool ProcessQueue(Dijkstra activeDijkstra, Dijkstra otherDijkstra, ref long meetingNode, ref float minDistance, bool isForward)
    {
        // Dequeue the closest node
        var currentNode = activeDijkstra.DequeueAndUpdateSets();

        // If the current node's distance is greater than or equal to the minimum known distance, stop
        if (activeDijkstra.distances[currentNode] >= minDistance)
        {
            return true;
        }

        // Get neighbors based on direction
        var neighbors = isForward ? graph.graph[currentNode] : graph.bi_graph[currentNode];

        // Process all neighbors
        activeDijkstra.UpdateNeighbors(currentNode, activeDijkstra.distances[currentNode], neighbors);

        foreach (var edge in neighbors)
        {
            var neighbor = edge.node;
            if (otherDijkstra.distances.ContainsKey(neighbor))
            {
                var potentialMinDistance = activeDijkstra.distances[currentNode] + edge.cost + otherDijkstra.distances[neighbor];
                if (potentialMinDistance < minDistance)
                {
                    minDistance = potentialMinDistance;
                    meetingNode = neighbor;
                }
            }
        }

        return false;
    }


    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        throw new NotImplementedException();
    }

    private Dictionary<long, long> MergePrevious(Dictionary<long, long> previous, Dictionary<long, long> previous2, long meetingPoint)
    {
        var merged = new Dictionary<long, long>(previous);

        // Start with the meeting point and work backwards towards the end
        long current = meetingPoint;
        while (previous2.ContainsKey(current) && previous2[current] != -1)
        {
            long next = previous2[current];
            // Invert the direction for the second half of the path
            merged[next] = current;
            current = next;
        }

        return merged;
    }
}
