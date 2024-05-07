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
        startNode = start;
        endNode = end;
        InitializeSearch(startNode, endNode);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (forwardAstar.openList.Count > 0 && backwardAstar.openList.Count > 0)
        {
            var distForward = forwardAstar.openList.First.Priority;
            var distBackward = backwardAstar.openList.First.Priority;
            var currentForward = forwardAstar.DequeueAndUpdateSets();
            var currentBackward = backwardAstar.DequeueAndUpdateSets();

            if (distForward + distBackward >= minDistance)
            {
                stopwatch.Stop();
                var allPrev = MergePrevious(forwardAstar.parent, backwardAstar.parent, meetingNode);
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
            ProcessQueue(backwardAstar, forwardAstar, currentBackward, end, false);
        }

        stopwatch.Stop();
        return null;
    }

    private void ProcessQueue(AStar activeAstar, AStar otherAstar, long current, long end, bool isForward)
    {
        // Get neighbors based on direction
        var neighbors = isForward ? graph.graph[current] : graph.bi_graph[current];
        activeAstar.UpdateNeighbors(current, end, neighbors);

        if (!otherAstar.closedSet.Contains(current)) return;

        var totalDist = activeAstar.gScore[current] + otherAstar.gScore[current];
        if (totalDist < minDistance)
        {
            minDistance = totalDist;
            meetingNode = current;
        }
    }


    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
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
