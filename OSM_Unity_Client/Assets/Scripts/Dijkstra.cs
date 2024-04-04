using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dijkstra : IPathfindingAlgorithm // Assuming this is attached to a GameObject
{
    public Graph graph;
    public Material lineMaterial; // Ensure this is set in the Inspector

    // Delegate and Event for when pathfinding completes
    public delegate void PathfindingComplete(float distance, long[] path);
    public event PathfindingComplete OnPathfindingComplete;

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


public IEnumerator<(float, long[])> FindShortestPathWithVisual(long start, long end)
    {
        var lineRenderer = Camera.main.GetComponent<GLLineRenderer>(); // Ensure Camera has GLLineRenderer component
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        var distances = new Dictionary<long, float>();
        var previous = new Dictionary<long, long>();
        var visited = new HashSet<long>();
        var queue = new SortedSet<(float, long)>();
        int nodesVisited = 0;


        foreach (var node in graph.nodes.Keys)
        {
            distances[node] = float.MaxValue;
            previous[node] = -1;
        }

        distances[start] = 0;
        queue.Add((0, start));

        while (queue.Count > 0)
        {
            var (distance, currentNode) = queue.Min;
            queue.Remove(queue.Min);
            if (visited.Contains(currentNode))
            {
                continue;
            }
            visited.Add(currentNode);

            if (currentNode == end)
            {
                stopwatch.Stop();
                Debug.Log($"Path found with distance {distance} in {stopwatch.ElapsedMilliseconds} ms");
                MapController.DisplayStatistics(start, end, distance, stopwatch.ElapsedMilliseconds, nodesVisited);
                OnPathfindingComplete?.Invoke(distance, MapController.ReconstructPath(previous, start, end));
                yield break;
            }

            foreach (var edge in graph.graph[currentNode])
            {
                nodesVisited++;
                var neighbor = edge.node;
                if (visited.Contains(neighbor)) continue;

                var newDistance = distance + edge.cost;
                if (newDistance < distances[neighbor])
                {
                    distances[neighbor] = newDistance;
                    previous[neighbor] = currentNode;
                    queue.Add((newDistance, neighbor));

                    // Visualization
                    var startCoord = graph.nodes[currentNode];
                    var endCoord = graph.nodes[neighbor];
                    lineRenderer.AddDiscoveryPath(new List<Vector3> { new Vector3(startCoord.Item1[0], startCoord.Item1[1], 0), new Vector3(endCoord.Item1[0], endCoord.Item1[1], 0) });
                    yield return (0,null); // Wait for next frame
                }
            }
        }

        OnPathfindingComplete?.Invoke(float.MaxValue, new long[0]);
    }
}
