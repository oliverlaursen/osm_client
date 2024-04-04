using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dijkstra : IPathfindingAlgorithm
{
    public Graph graph;
    public Material lineMaterial; // Ensure this is set in the Inspector

    public Dijkstra(Graph graph)
    {
        this.graph = graph;
    }

    private void InitializeDijkstra(long start, out Dictionary<long, float> distances, out Dictionary<long, long> previous, out HashSet<long> visited, out SortedSet<(float, long)> queue)
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
        InitializeDijkstra(start, out var distances, out var previous, out var visited, out var queue);
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

            UpdateNeighbors(currentNode, distance, graph.graph[currentNode], ref distances, ref previous, ref queue, ref nodesVisited, visited);
        }

        return;
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end)
    {
        InitializeDijkstra(start, out var distances, out var previous, out var visited, out var queue);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
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

            UpdateNeighbors(currentNode, distance, graph.graph[currentNode], ref distances, ref previous, ref queue, ref nodesVisited, visited, lineRenderer);
            yield return null; // Wait for next frame
        }
    }

    private void UpdateNeighbors(long currentNode, float distance, IEnumerable<Edge> neighbors, ref Dictionary<long, float> distances, ref Dictionary<long, long> previous, ref SortedSet<(float, long)> queue, ref int nodesVisited, HashSet<long> visited, GLLineRenderer lineRenderer = null)
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
