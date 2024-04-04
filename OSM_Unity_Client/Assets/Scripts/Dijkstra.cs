using System;
using System.Collections.Generic;
using UnityEngine;

public class Dijkstra : IPathfindingAlgorithm
{
    public Graph graph;

    public Dijkstra(Graph graph)
    {
        this.graph = graph;
    }

    public (float, long[]) FindShortestPath(long start, long end)
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
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

            if (node == end)
            {
                stopwatch.Stop();
                MapController.DisplayStatistics(start, end, distance, stopwatch.ElapsedMilliseconds, nodesVisited);
                UnityEngine.Debug.Log("nodes visited " + nodesVisited);
                return (distance, MapController.ReconstructPath(previous, start, end));
            }

            foreach (var edge in edges[node])
            {
                nodesVisited++;
                var firstCoord = new Vector3(nodes[node][0], nodes[node][1], 0);
                var secondCoord = new Vector3(nodes[edge.node][0], nodes[edge.node][1], 0);

                Debug.DrawLine(firstCoord, secondCoord, Color.green, 0.0f);

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
}

