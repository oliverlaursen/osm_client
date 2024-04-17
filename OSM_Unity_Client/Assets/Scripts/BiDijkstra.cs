using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BiDijkstra : IPathfindingAlgorithm
{
    public Graph graph;
    private Dijkstra dijkstra1;
    private Dijkstra dijkstra2;

    public BiDijkstra(Graph graph)
    {
        this.graph = graph;
        dijkstra1 = new Dijkstra(graph);
        dijkstra2 = new Dijkstra(graph);
    }

    public void FindShortestPath(long start, long end)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int nodesVisited = 0;
        dijkstra1.InitializeDijkstra(start, graph);
        dijkstra2.InitializeDijkstra(end, graph);

        double shortestDistance = double.PositiveInfinity;
        long meetingNode = -1;

        while (dijkstra1.queue.Count > 0 && dijkstra2.queue.Count > 0)
        {
            if (dijkstra1.queue.Min.Item1 + dijkstra2.queue.Min.Item1 >= shortestDistance)
            {
                stopwatch.Stop();
                MapController.DisplayStatistics(start, end, (float)shortestDistance, stopwatch.ElapsedMilliseconds, nodesVisited);
                var allPrev = MergePrevious(dijkstra1.previous, dijkstra2.previous, meetingNode);
                GameObject.Find("Map").GetComponent<MapController>().DrawPath(graph.nodes, MapController.ReconstructPath(allPrev, start, end));
                return;
            }
            UpdateBiNeighbors(ref nodesVisited, ref shortestDistance, ref meetingNode, null);
        }
    }

    public void UpdateBiNeighbors(ref int nodesVisited, ref double shortestDistance, ref long meetingNode, GLLineRenderer lineRenderer)
    {
        var (distance, currentNode) = dijkstra1.queue.Min;
        var (distance2, currentNode2) = dijkstra2.queue.Min;
        dijkstra1.queue.Remove(dijkstra1.queue.Min);
        if (!dijkstra1.visited.Add(currentNode)) return;
        // when scanning an arc (v, w) in the forward search and w is scanned in the reverse search,
        // update µ if df (v) + l(v, w) + dr(w) < µ.
        if (dijkstra2.visited.Contains(currentNode) && distance + dijkstra2.distances[currentNode] < shortestDistance)
        {
            shortestDistance = distance + dijkstra2.distances[currentNode];
            meetingNode = currentNode;
        }

        dijkstra1.UpdateNeighbors(currentNode, distance, graph.graph[currentNode], ref nodesVisited, lineRenderer);

        dijkstra2.queue.Remove(dijkstra2.queue.Min);
        if (!dijkstra2.visited.Add(currentNode2)) return;

        if (dijkstra1.visited.Contains(currentNode2) && distance2 + dijkstra1.distances[currentNode2] < shortestDistance)
        {
            shortestDistance = distance2 + dijkstra1.distances[currentNode2];
            meetingNode = currentNode2;
        }
        var neighbors = graph.bi_graph[currentNode2];
        dijkstra2.UpdateNeighbors(currentNode2, distance2, neighbors, ref nodesVisited, lineRenderer);

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
        dijkstra1.InitializeDijkstra(start, graph);
        dijkstra2.InitializeDijkstra(end, graph);
        var lineRenderer = Camera.main.GetComponent<GLLineRenderer>();

        double shortestDistance = double.PositiveInfinity;
        long meetingNode = -1;

        while (dijkstra1.queue.Count > 0 && dijkstra2.queue.Count > 0)
        {

            if (dijkstra1.queue.Max.Item1 + dijkstra2.queue.Max.Item1 >= shortestDistance)
            {
                stopwatch.Stop();
                MapController.DisplayStatistics(start, end, (float)shortestDistance, stopwatch.ElapsedMilliseconds, nodesVisited);
                var allPrev = MergePrevious(dijkstra1.previous, dijkstra2.previous, meetingNode);
                lineRenderer.ClearDiscoveryPath();
                GameObject.Find("Map").GetComponent<MapController>().DrawPath(graph.nodes, MapController.ReconstructPath(allPrev, start, end));
                yield break;
            }
            else
            {
                UpdateBiNeighbors(ref nodesVisited, ref shortestDistance, ref meetingNode, lineRenderer);
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
