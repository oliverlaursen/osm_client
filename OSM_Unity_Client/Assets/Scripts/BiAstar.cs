using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Antlr3.Runtime;
using UnityEngine;


public class BiAStar : IPathfindingAlgorithm
{
    public Graph graph;
    private AStar forwardAstar;
    private AStar backwardAstar;
    private long meetingNode;
    private float minDistance;
    private long startNode, endNode;

    public BiAStar(Graph graph)
    {
        this.graph = graph;
        forwardAstar = new AStar(graph);
        backwardAstar = new AStar(graph);

    }

    private void InitializeSearch(long start, long end)
    {
        forwardAstar.InitializeSearch(start, end);
        backwardAstar.InitializeSearch(end, start);
        minDistance = float.PositiveInfinity;
        meetingNode = -1;
    }

    public PathResult FindShortestPath(long start, long end)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        startNode = start;
        endNode = end;
        InitializeSearch(startNode, endNode);

        while (forwardAstar.queue.Count > 0 && backwardAstar.queue.Count > 0)
        {
            var distForward = forwardAstar.queue.First.Priority;
            var distBackward = backwardAstar.queue.First.Priority;
            var currentForward = forwardAstar.queue.Dequeue().Id;
            var currentBackward = backwardAstar.queue.Dequeue().Id;

            var distSum = distBackward + distForward;
            var backwardsHeuristic = backwardAstar.heuristic.Calculate(currentBackward, startNode);

            if (distSum >= minDistance + backwardsHeuristic)
            {
                stopwatch.Stop();
                var allPrev = MergePrevious(forwardAstar.previous, backwardAstar.previous, meetingNode);
                var path = MapController.ReconstructPath(allPrev, start, end);
                // Recaculate distance from path
                var distance = 0f;
                for (int i = 0; i < path.Length - 1; i++)
                {
                    distance += Array.Find(graph.graph[path[i]], edge => edge.node == path[i + 1]).cost;
                }
                return new PathResult(start, end, distance, stopwatch.ElapsedMilliseconds, forwardAstar.nodesVisited + backwardAstar.nodesVisited, path);
            }
            forwardAstar.closedSet.Add(currentForward);
            ProcessQueue(forwardAstar, backwardAstar, currentForward, end, true);
            backwardAstar.closedSet.Add(currentBackward);
            ProcessQueue(backwardAstar, forwardAstar, currentBackward, start, false);
        }

        stopwatch.Stop();
        return null;
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        var lineRenderer = Camera.main.GetComponent<GLLineRenderer>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
        startNode = start;
        endNode = end;
        InitializeSearch(startNode, endNode);

        while (forwardAstar.queue.Count > 0 && backwardAstar.queue.Count > 0)
        {
            var distForward = forwardAstar.queue.First.Priority;
            var distBackward = backwardAstar.queue.First.Priority;
            var currentForward = forwardAstar.queue.Dequeue().Id;
            var currentBackward = backwardAstar.queue.Dequeue().Id;

            var distSum = distBackward + distForward;
            var backwardsHeuristic = backwardAstar.heuristic.Calculate(currentBackward, startNode);

            if (distSum >= minDistance + backwardsHeuristic)
            {
                stopwatch.Stop();
                lineRenderer.ClearDiscoveryPath();
                var allPrev = MergePrevious(forwardAstar.previous, backwardAstar.previous, meetingNode);
                var path = MapController.ReconstructPath(allPrev, start, end);
                // Recaculate distance from path
                var distance = 0f;
                for (int i = 0; i < path.Length - 1; i++)
                {
                    distance += Array.Find(graph.graph[path[i]], edge => edge.node == path[i + 1]).cost;
                }
                var result = new PathResult(start, end, distance, stopwatch.ElapsedMilliseconds, forwardAstar.nodesVisited + backwardAstar.nodesVisited, path);
                result.DisplayAndDrawPath(graph);
                yield break;
            }
            forwardAstar.closedSet.Add(currentForward);
            ProcessQueue(forwardAstar, backwardAstar, currentForward, end, true, lineRenderer);
            backwardAstar.closedSet.Add(currentBackward);
            ProcessQueue(backwardAstar, forwardAstar, currentBackward, start, false, lineRenderer);
            if (drawspeed == 0) yield return null;
            else if (stopwatch2.ElapsedTicks > drawspeed)
            {
                yield return null;
                stopwatch2.Restart();
            }
        }

        yield return null;
    }

    private void ProcessQueue(AStar activeAstar, AStar otherAstar, long current, long end, bool isForward, GLLineRenderer lineRenderer = null)
    {

        // Get neighbors based on direction
        var neighbors = isForward ? graph.graph[current] : graph.bi_graph[current];
        if (lineRenderer == null)
        {
            activeAstar.UpdateNeighbors(current, end, neighbors);
        } else {
            activeAstar.UpdateNeighborsWithVisual(current, end, neighbors, lineRenderer);
        }

        if (!otherAstar.closedSet.Contains(current)) return;

        var totalDist = activeAstar.gScore[current] + otherAstar.gScore[current];
        if (totalDist < minDistance)
        {
            minDistance = totalDist;
            meetingNode = current;
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
}
