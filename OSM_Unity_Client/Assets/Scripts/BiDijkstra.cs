using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Codice.CM.Client.Differences.Merge;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class BiDijkstra : IPathfindingAlgorithm
{
    public Graph graph;
    private Dijkstra dijkstra1;
    private Dijkstra dijkstra2;
    long meetingNode = -1;

    public BiDijkstra(Graph graph)
    {
        this.graph = graph;
        dijkstra1 = new Dijkstra(graph);
        dijkstra2 = new Dijkstra(graph);
    }

    public void InitializeSearch()
    {

    }

    public PathResult FindShortestPath(long start, long end)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        dijkstra1.InitializeDijkstra(start, graph);
        dijkstra2.InitializeDijkstra(end, graph);

        double u = double.PositiveInfinity;

        while (dijkstra1.queue.Count > 0 && dijkstra2.queue.Count > 0)
        {
            // v_f: vertex from forward search
            // d_f: distance from start to v_f
            // u: shortest distance found so far
            // same for reverse search
            var v_f = dijkstra1.queue.Dequeue().Id;
            var d_f = dijkstra1.distances[v_f];

            var v_r = dijkstra2.queue.Dequeue().Id;
            var d_r = dijkstra2.distances[v_r];

            if (d_f + d_r >= u )
            {
                Debug.Log("Distance1: " + d_f + " Distance2: " + d_r + " ShortestDistance: " + u);
                stopwatch.Stop();
                var allPrev = MergePrevious(dijkstra1.previous, dijkstra2.previous, meetingNode);
                int nodesVisited = dijkstra1.nodesVisited + dijkstra2.nodesVisited;
                return new PathResult(start, end, (float)u, stopwatch.ElapsedMilliseconds, nodesVisited, MapController.ReconstructPath(allPrev, start, end));
            }
            UpdateBiNeighbors(v_f, ref u, d_f, v_r, d_r,null);
        }
        return null;
    }

    public void UpdateBiNeighbors(long v_f, ref double u, float d_f, long v_r, float d_r, GLLineRenderer lineRenderer)
    {
        dijkstra1.visited.Add(v_f);
        dijkstra2.visited.Add(v_r);
        dijkstra1.UpdateNeighbors(v_f, d_f, graph.graph[v_f], lineRenderer);
        dijkstra2.UpdateNeighbors(v_r, d_r, graph.bi_graph[v_r], lineRenderer);
        if (!dijkstra1.visited.Contains(v_r) || !dijkstra2.visited.Contains(v_f)) return;

        
        var prev_f = dijkstra1.previous[v_f];
        var prev_r = dijkstra2.previous[v_r];
        var l_f = graph.graph[prev_f].Where(x => x.node == v_f).Select(x => x.cost).First();
        var l_r = graph.bi_graph[prev_r].Where(x => x.node == v_r).Select(x => x.cost).First();

        // when scanning an arc (v, w) in the forward search and w is scanned in the reverse search,
        // update µ if df (v) + l(v, w) + dr(w) < µ.
        if (dijkstra1.distances[prev_f] + l_f + dijkstra2.distances[v_f] < u)
        {
            u = dijkstra1.distances[prev_f] + l_f + dijkstra2.distances[v_f];
            meetingNode = v_f;
        }

        if (dijkstra2.distances[prev_r] + l_r + dijkstra1.distances[v_r] < u)
        {
            u = dijkstra2.distances[prev_r] + l_r + dijkstra1.distances[v_r];
            meetingNode = v_r;
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

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();

        double shortestDistance = double.PositiveInfinity;
        dijkstra1.InitializeDijkstra(start, graph);
        dijkstra2.InitializeDijkstra(end, graph);
        var lineRenderer = Camera.main.GetComponent<GLLineRenderer>();


        while (dijkstra1.queue.Count > 0 && dijkstra2.queue.Count > 0)
        {
            var currentNode = dijkstra1.queue.Dequeue().Id;
            var distance = dijkstra1.distances[currentNode];
            var currentNode2 = dijkstra2.queue.Dequeue().Id;
            var distance2 = dijkstra2.distances[currentNode2];


            if (distance + distance2 >= shortestDistance)
            {
                stopwatch.Stop();
                var allPrev = MergePrevious(dijkstra1.previous, dijkstra2.previous, meetingNode);
                lineRenderer.ClearDiscoveryPath();
                int nodesVisited = dijkstra1.nodesVisited + dijkstra2.nodesVisited;
                var result = new PathResult(start, end, (float)shortestDistance, stopwatch.ElapsedMilliseconds, nodesVisited, MapController.ReconstructPath(allPrev, start, end));
                result.DisplayAndDrawPath(graph);
                yield break;
            }
            else
            {
                UpdateBiNeighbors(currentNode, ref shortestDistance, distance, currentNode2, distance2,lineRenderer);
                if (drawspeed == 0) yield return null;
                else if (stopwatch2.ElapsedTicks > drawspeed)
                {
                    yield return null;
                    stopwatch2.Restart();
                }
            }
        }
    }
}
