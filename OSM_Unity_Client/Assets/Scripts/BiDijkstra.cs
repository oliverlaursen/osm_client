using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BiDijkstra : MonoBehaviour, IPathfindingAlgorithm
{
    public Graph graph;
    private Dijkstra forwardDijkstra;
    private Dijkstra backwardDijkstra;
    private long meetingNode;
    private float minDistance;
    private long startNode, endNode;

    public BiDijkstra(Graph graph)
    {
        this.graph = graph;
        forwardDijkstra = new Dijkstra(graph);
        backwardDijkstra = new Dijkstra(graph);
    }

    private void Initialize(long start, long end, Graph graph)
    {
        forwardDijkstra.InitializeSearch(start, graph);
        backwardDijkstra.InitializeSearch(end, graph);
        minDistance = float.PositiveInfinity;
        meetingNode = -1;
    }

    public PathResult FindShortestPath(long start, long end)
    {
        startNode = start;
        endNode = end;
        Initialize(start, end, graph);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (forwardDijkstra.queue.Count > 0 && backwardDijkstra.queue.Count > 0)
        {
            // Stopping condition
            var topf = forwardDijkstra.queue.First.Priority;
            var topr = backwardDijkstra.queue.First.Priority;
            if (topf + topr >= minDistance)
            {
                stopwatch.Stop();
                var allPrev = MergePrevious(forwardDijkstra.previous, backwardDijkstra.previous, meetingNode);
                var path = MapController.ReconstructPath(allPrev, start, end);
                // Recaculate distance from path
                var distance = 0f;
                for (int i = 0; i < path.Length - 1; i++)
                {
                    distance += Array.Find(graph.graph[path[i]], edge => edge.node == path[i + 1]).cost;
                }
                return new PathResult(start, end, distance, stopwatch.ElapsedMilliseconds, forwardDijkstra.nodesVisited + backwardDijkstra.nodesVisited, path);
            }

            // Process forward direction
            ProcessQueue(forwardDijkstra, backwardDijkstra, ref meetingNode, ref minDistance, true);
            
            // Process backward direction
            ProcessQueue(backwardDijkstra, forwardDijkstra, ref meetingNode, ref minDistance, false);
            
        }

        stopwatch.Stop();
        return null;
    }

    private bool ProcessQueue(Dijkstra activeDijkstra, Dijkstra otherDijkstra, ref long meetingNode, ref float minDistance, bool isForward, GLLineRenderer lineRenderer = null)
    {
        // Dequeue the closest node
        var currentNode = activeDijkstra.DequeueAndUpdateSets();

        // If the current node's distance is greater than or equal to the minimum known distance, stop
        if (activeDijkstra.distances[currentNode] >= minDistance)
        {
            return true;
        }

        // Get neighbors based on direction
        var neighbors = isForward ? graph.graph[currentNode] : graph.bi_graph[currentNode];

        // Process all neighbors
        activeDijkstra.UpdateNeighbors(currentNode, activeDijkstra.distances[currentNode], neighbors);

        foreach (var edge in neighbors)
        {
            var neighbor = edge.node;
            if (otherDijkstra.distances.ContainsKey(neighbor))
            {
                var potentialMinDistance = activeDijkstra.distances[currentNode] + edge.cost + otherDijkstra.distances[neighbor];
                if (potentialMinDistance < minDistance)
                {
                    minDistance = potentialMinDistance;
                    meetingNode = neighbor;
                }
            }
            if (lineRenderer != null)
            {
                var startCoord = graph.nodes[currentNode];
                var endCoord = graph.nodes[neighbor];
                lineRenderer.AddDiscoveryPath(new List<Vector3> { new Vector3(startCoord.Item1[0], startCoord.Item1[1], 0), new Vector3(endCoord.Item1[0], endCoord.Item1[1], 0) });
            }
        }

        return false;
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        startNode = start;
        endNode = end;
        Initialize(start, end, graph);
        var lineRenderer = Camera.main.GetComponent<GLLineRenderer>(); // Ensure Camera has GLLineRenderer component
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();

        while (forwardDijkstra.queue.Count > 0 && backwardDijkstra.queue.Count > 0)
        {
            // Stopping condition
            var topf = forwardDijkstra.queue.First.Priority;
            var topr = backwardDijkstra.queue.First.Priority;
            if (topf + topr >= minDistance)
            {
                stopwatch.Stop();
                lineRenderer.ClearDiscoveryPath();
                var path = MergePrevious(forwardDijkstra.previous, backwardDijkstra.previous, meetingNode);
                var result = new PathResult(start, end, minDistance, stopwatch.ElapsedMilliseconds, forwardDijkstra.nodesVisited + backwardDijkstra.nodesVisited, MapController.ReconstructPath(path, start, end));
                result.DisplayAndDrawPath(graph);
                yield break;
            }

            // Process forward direction
            if (ProcessQueue(forwardDijkstra, backwardDijkstra, ref meetingNode, ref minDistance, true, lineRenderer))
            {
                stopwatch.Stop();
                lineRenderer.ClearDiscoveryPath();
                var path = MergePrevious(forwardDijkstra.previous, backwardDijkstra.previous, meetingNode);
                var result = new PathResult(start, end, minDistance, stopwatch.ElapsedMilliseconds, forwardDijkstra.nodesVisited + backwardDijkstra.nodesVisited, MapController.ReconstructPath(path, start, end));
                result.DisplayAndDrawPath(graph);
                yield break;
            }

            // Process backward direction
            if (ProcessQueue(backwardDijkstra, forwardDijkstra, ref meetingNode, ref minDistance, false, lineRenderer))
            {
                stopwatch.Stop();
                lineRenderer.ClearDiscoveryPath();
                var path = MergePrevious(forwardDijkstra.previous, backwardDijkstra.previous, meetingNode);
                var result = new PathResult(start, end, minDistance, stopwatch.ElapsedMilliseconds, forwardDijkstra.nodesVisited + backwardDijkstra.nodesVisited, MapController.ReconstructPath(path, start, end));
                result.DisplayAndDrawPath(graph);
                yield break;
            }

            // Drawing at intervals
            if (drawspeed == 0) yield return null;
            else if (stopwatch2.ElapsedTicks > drawspeed)
            {
                yield return null;
                stopwatch2.Restart();
            }
        }

        stopwatch.Stop();
        yield return null;
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
