using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;
using Unity.VisualScripting;

public class Dijkstra : IPathfindingAlgorithm
{
    public Graph graph;
    public FastPriorityQueue<PriorityQueueNode> openList;
    public Dictionary<long, float> distances;
    public Dictionary<long, long> previous;
    public HashSet<long> visited;
    public SortedSet<(float, long)> queue;

    public Dijkstra(Graph graph)
    {
        this.graph = graph;

    }

    public void InitializeDijkstra(long start, Graph graph)
    {
        distances = new Dictionary<long, float>();
        previous = new Dictionary<long, long>();
        visited = new HashSet<long>();
        queue = new SortedSet<(float, long)>();

        foreach (var node in graph.nodes.Keys)
        {
            distances[node] = float.MaxValue;
            previous[node] = -1;
        }
        distances[start] = 0;
        queue.Add((0, start));
    }

    public void FindShortestPath(long start, long end)
    {
        InitializeDijkstra(start, graph);
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

            var neighbors = graph.graph[currentNode];
            UpdateNeighbors(currentNode, distance, neighbors, ref nodesVisited);
        }

        return;
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        InitializeDijkstra(start, graph);
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

            var neighbors = graph.graph[currentNode];
            UpdateNeighbors(currentNode, distance, neighbors, ref nodesVisited, lineRenderer);
            if (drawspeed == 0) yield return null;
                else if (stopwatch2.ElapsedTicks > drawspeed)
                {
                    yield return null;
                    stopwatch2.Restart();
                }
        }
    }

    public void UpdateNeighbors(long currentNode, float distance, IEnumerable<Edge> neighbors, ref int nodesVisited, GLLineRenderer lineRenderer = null)
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
