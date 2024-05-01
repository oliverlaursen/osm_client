using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using PlasticPipe.PlasticProtocol.Messages;
using Unity.Plastic.Antlr3.Runtime;
using Unity.VisualScripting;
using UnityEngine;

public class BiDijkstra : IPathfindingAlgorithm
{
    public Graph graph;
    private Dijkstra dijkstra1;
    private Dijkstra dijkstra2;
    private long joinNode; //node where the forward dijkstra and the backward dijkstra meet
    private float shortestDistance; //shortest distance seen so far

    public BiDijkstra(Graph graph)
    {
        this.graph = graph;
        dijkstra1 = new Dijkstra(graph);
        dijkstra2 = new Dijkstra(graph);
    }

    public void InitializeSearch(long start, long end)
    {
        dijkstra1.InitializeSearch(start, graph);
        dijkstra2.InitializeSearch(end, graph);

        joinNode = -1;
        shortestDistance = float.PositiveInfinity;

    }

    public PathResult FindShortestPath(long start, long end)
    {
        InitializeSearch(start, end);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (dijkstra1.queue.Count > 0 && dijkstra2.queue.Count > 0)
        {
            var firstForward = dijkstra1.queue.First.Id;
            var firstBackward = dijkstra2.queue.First.Id;
            var forwardDist = dijkstra1.distances[firstForward];
            var backwardDist = dijkstra2.distances[firstBackward];

            var stopCond = forwardDist + backwardDist >= shortestDistance;
            if (stopCond)
            {
                stopwatch.Stop();
                var nodesVisited = dijkstra1.nodesVisited + dijkstra2.nodesVisited;
                var mergedPath = MergePrevious(dijkstra1.previous, dijkstra2.previous, joinNode);
                return new PathResult(start, end, shortestDistance, stopwatch.ElapsedMilliseconds, nodesVisited, MapController.ReconstructPath(mergedPath, start, end));
            }

            long currentNode;
            float dist;
            if (forwardDist < backwardDist)
            {
                currentNode = dijkstra1.DequeueAndUpdateSets();
                dist = dijkstra1.distances[currentNode];
                UpdateBiNeighbors(currentNode, dist, true);
            }
            else
            {
                currentNode = dijkstra2.DequeueAndUpdateSets();
                dist = dijkstra2.distances[currentNode];
                UpdateBiNeighbors(currentNode, dist, false);
            }
        }
        return null;
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        throw new NotImplementedException();
    }

    public void UpdateBiNeighbors(long currentNode, float dist, bool forward)
    {
        if (forward)
        {
            if (!dijkstra1.visited.Add(currentNode)) return;
            if (dijkstra2.visited.Contains(currentNode) && dist + dijkstra2.distances[currentNode] < shortestDistance)
            {
                shortestDistance = dist + dijkstra2.distances[currentNode];
                joinNode = currentNode;
            }
            var neighbors = graph.graph[currentNode];
            dijkstra1.UpdateNeighbors(currentNode, dist, neighbors);
        }
        else
        {
            if (!dijkstra2.visited.Add(currentNode)) return;
            if (dijkstra1.visited.Contains(currentNode) && dist + dijkstra1.distances[currentNode] < shortestDistance)
            {
                shortestDistance = dist + dijkstra1.distances[currentNode];
                joinNode = currentNode;
            }
            var neighbors = graph.bi_graph[currentNode];
            dijkstra2.UpdateNeighbors(currentNode, dist, neighbors);
        }
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