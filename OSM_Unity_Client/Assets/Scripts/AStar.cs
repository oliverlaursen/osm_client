using System;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine.Assertions;

public class AStar : IPathfindingAlgorithm
{
    public Graph graph;

    public AStar(Graph graph)
    {
        this.graph = graph;
    }

    public (float, long[]) FindShortestPath(long start, long end)
    {
        // Assuming SimplePriorityQueue is similar to PriorityQueue in .NET 6+, 
        // which does not have a Contains method, thus maintaining a HashSet for open set tracking
        var openList = new SimplePriorityQueue<long, float>();
        var openSet = new HashSet<long>(); // Tracks the items currently in the open list
        var closedSet = new HashSet<long>();
        var parent = new Dictionary<long, long>();
        var gScore = new Dictionary<long, float>() { [start] = 0 };
        var fScore = new Dictionary<long, float>() { [start] = HeuristicCostEstimate(start, end) };

        int nodesVisited = 0;
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        openList.Enqueue(start, fScore[start]);
        openSet.Add(start);

        while (openList.Count > 0)
        {
            long current = openList.Dequeue();
            openSet.Remove(current);

            if (current == end)
            {
                stopwatch.Stop();
                MapController.DisplayStatistics(start, end, gScore[current], stopwatch.ElapsedMilliseconds, nodesVisited);
                return (gScore[current], MapController.ReconstructPath(parent, start, end));
            }
            nodesVisited++;

            closedSet.Add(current);

            foreach (var neighbor in graph.GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor.node))
                    continue;

                float tentativeGScore = gScore[current] + neighbor.cost;

                if (!gScore.ContainsKey(neighbor.node) || tentativeGScore < gScore[neighbor.node])
                {
                    // This path to neighbor is better than any previous one. 
                    parent[neighbor.node] = current;
                    gScore[neighbor.node] = tentativeGScore;
                    fScore[neighbor.node] = tentativeGScore + HeuristicCostEstimate(neighbor.node, end);

                    if (!openSet.Contains(neighbor.node))
                    {
                        openList.Enqueue(neighbor.node, fScore[neighbor.node]);
                        openSet.Add(neighbor.node);
                    }
                    else
                    {
                        openList.UpdatePriority(neighbor.node, fScore[neighbor.node]);
                    }
                }
            }
        }

        // If the goal is never reached
        return (float.MaxValue, Array.Empty<long>());
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