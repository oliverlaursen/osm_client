using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dijkstra : IPathfindingAlgorithm
{
    public Graph graph;

    public Dijkstra(Graph graph)
    {
        this.graph = graph;
    }

    public static (Dictionary<long, float> distances, Dictionary<long, long> previous, HashSet<long> visited, SortedSet<(float, long)> queue) InitializeDijkstra(long start, Graph graph)
    {
        var distances = new Dictionary<long, float>();
        var previous = new Dictionary<long, long>();
        var visited = new HashSet<long>();
        var queue = new SortedSet<(float, long)>();

        foreach (var node in graph.nodes.Keys)
        {
            distances[node] = float.MaxValue;
            previous[node] = -1;
        }
        distances[start] = 0;
        queue.Add((0, start));

        return (distances, previous, visited, queue);
    }

    public void FindShortestPath(long start, long end)
    {
        (var distances, var previous, var visited, var queue) = InitializeDijkstra(start, graph);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int nodesVisited = 0;

        while (queue.Count > 0)
        {
            var (distance, currentNode) = queue.Min;
            queue.Remove(queue.Min);
            if (!visited.Add(currentNode)) continue;

            if (currentNode == end)
            {
                stopwatch.Stop();
                MapController.DisplayStatistics(start, end, distance, stopwatch.ElapsedMilliseconds, nodesVisited);
                GameObject.Find("Map").GetComponent<MapController>().DrawPath(graph.nodes, MapController.ReconstructPath(previous, start, end));
                return;
            }

            UpdateNeighbors(currentNode, distance, graph.graph[currentNode], ref distances, ref previous, ref queue, ref nodesVisited, visited,graph);
        }

        return;
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        (var distances, var previous, var visited, var queue) = InitializeDijkstra(start, graph);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
        var lineRenderer = Camera.main.GetComponent<GLLineRenderer>(); // Ensure Camera has GLLineRenderer component
        int nodesVisited = 0;

        while (queue.Count > 0) 
        {
            var (distance, currentNode) = queue.Min;
            queue.Remove(queue.Min);
            if (!visited.Add(currentNode)) continue;

            if (currentNode == end)
            {
                stopwatch.Stop();
                MapController.DisplayStatistics(start, end, distance, stopwatch.ElapsedMilliseconds, nodesVisited);
                lineRenderer.ClearDiscoveryPath();
                GameObject.Find("Map").GetComponent<MapController>().DrawPath(graph.nodes, MapController.ReconstructPath(previous, start, end));
                yield break;
            }

            UpdateNeighbors(currentNode, distance, graph.graph[currentNode], ref distances, ref previous, ref queue, ref nodesVisited, visited, graph, lineRenderer);
            if (drawspeed == 0) yield return null;
                else if (stopwatch2.ElapsedMilliseconds > drawspeed)
                {
                    stopwatch2.Restart();
                    yield return null;
                }
        }
    }

    public static void UpdateNeighbors(long currentNode, float distance, IEnumerable<Edge> neighbors, ref Dictionary<long, float> distances, ref Dictionary<long, long> previous, ref SortedSet<(float, long)> queue, ref int nodesVisited, HashSet<long> visited, Graph graph, GLLineRenderer lineRenderer = null)
    {
        foreach (var edge in neighbors)
        {
            nodesVisited++;
            var neighbor = edge.node;
            var newDistance = distance + edge.cost;
            if (newDistance < distances[neighbor])
            {
                distances[neighbor] = newDistance;
                previous[neighbor] = currentNode;
                queue.Add((newDistance, neighbor));

                if (lineRenderer != null)
                {
                    var startCoord = graph.nodes[currentNode];
                    var endCoord = graph.nodes[neighbor];
                    lineRenderer.AddDiscoveryPath(new List<Vector3> { new(startCoord.Item1[0], startCoord.Item1[1], 0), new(endCoord.Item1[0], endCoord.Item1[1], 0) });
                }
            }
        }
    }
}
