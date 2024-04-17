using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BiDijkstra : IPathfindingAlgorithm
{
    public Graph graph;

    public BiDijkstra(Graph graph)
    {
        this.graph = graph;
    }

    public void FindShortestPath(long start, long end)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int nodesVisited = 0;
        (var distances, var previous, var visited, var queue) = Dijkstra.InitializeDijkstra(start, graph);
        (var distances2, var previous2, var visited2, var queue2) = Dijkstra.InitializeDijkstra(end, graph);

        double shortestDistance = double.PositiveInfinity;
        long meetingNode = -1;

        while (queue.Count > 0 && queue2.Count > 0)
        {
            var (distance, currentNode) = queue.Min;
            var (distance2, currentNode2) = queue2.Min;

            if (queue.Max.Item1 + queue2.Max.Item1 >= shortestDistance)
            {
                stopwatch.Stop();
                MapController.DisplayStatistics(start, end, (float)shortestDistance, stopwatch.ElapsedMilliseconds, nodesVisited);
                var allPrev = MergePrevious(previous, previous2, meetingNode);
                GameObject.Find("Map").GetComponent<MapController>().DrawPath(graph.nodes, MapController.ReconstructPath(allPrev, start, end));
                return;
            }
            UpdateBiNeighbors(ref distances, ref previous, ref visited, ref queue, ref distances2, ref previous2, ref visited2, ref queue2, ref nodesVisited, ref shortestDistance, ref meetingNode, ref currentNode, ref currentNode2, ref distance, ref distance2, null);
        }
    }

    public void UpdateBiNeighbors(ref Dictionary<long, float> distances, ref Dictionary<long, long> previous, ref HashSet<long> visited, ref SortedSet<(float, long)> queue, ref Dictionary<long, float> distances2, ref Dictionary<long, long> previous2, ref HashSet<long> visited2, ref SortedSet<(float, long)> queue2, ref int nodesVisited, ref double shortestDistance, ref long meetingNode, ref long currentNode, ref long currentNode2, ref float distance, ref float distance2, GLLineRenderer lineRenderer)
    {
        queue.Remove(queue.Min);
        if (!visited.Add(currentNode)) return;
        // when scanning an arc (v, w) in the forward search and w is scanned in the reverse search,
        // update µ if df (v) + l(v, w) + dr(w) < µ.
        if (visited2.Contains(currentNode) && distance + distances2[currentNode] < shortestDistance)
        {
            shortestDistance = distance + distances2[currentNode];
            meetingNode = currentNode;
        }

        Dijkstra.UpdateNeighbors(currentNode, distance, graph.graph[currentNode], ref distances, ref previous, ref queue, ref nodesVisited, visited, graph, lineRenderer);

        queue2.Remove(queue2.Min);
        if (!visited2.Add(currentNode2)) return;

        if (visited.Contains(currentNode2) && distance2 + distances[currentNode2] < shortestDistance)
        {
            shortestDistance = distance2 + distances[currentNode2];
            meetingNode = currentNode2;
        }

        Dijkstra.UpdateNeighbors(currentNode2, distance2, graph.bi_graph[currentNode2], ref distances2, ref previous2, ref queue2, ref nodesVisited, visited2, graph, lineRenderer);

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

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
        int nodesVisited = 0;
        (var distances, var previous, var visited, var queue) = Dijkstra.InitializeDijkstra(start, graph);
        (var distances2, var previous2, var visited2, var queue2) = Dijkstra.InitializeDijkstra(end, graph);
        var lineRenderer = Camera.main.GetComponent<GLLineRenderer>();

        double shortestDistance = double.PositiveInfinity;
        long meetingNode = -1;

        while (queue.Count > 0 && queue2.Count > 0)
        {
            var (distance, currentNode) = queue.Min;
            var (distance2, currentNode2) = queue2.Min;

            if (queue.Max.Item1 + queue2.Max.Item1 >= shortestDistance)
            {
                stopwatch.Stop();
                MapController.DisplayStatistics(start, end, (float)shortestDistance, stopwatch.ElapsedMilliseconds, nodesVisited);
                var allPrev = MergePrevious(previous, previous2, meetingNode);
                lineRenderer.ClearDiscoveryPath();
                GameObject.Find("Map").GetComponent<MapController>().DrawPath(graph.nodes, MapController.ReconstructPath(allPrev, start, end));
                yield break;
            }
            else
            {
                UpdateBiNeighbors(ref distances, ref previous, ref visited, ref queue, ref distances2, ref previous2, ref visited2, ref queue2, ref nodesVisited, ref shortestDistance, ref meetingNode, ref currentNode, ref currentNode2, ref distance, ref distance2, lineRenderer);
                if (drawspeed == 0) yield return null;
                else if (stopwatch2.ElapsedMilliseconds > drawspeed)
                {
                    stopwatch2.Restart();
                    yield return null;
                }
            }
        }
    }
}
