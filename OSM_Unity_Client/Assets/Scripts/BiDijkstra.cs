using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BiDijkstra : MonoBehaviour, IPathfindingAlgorithm
{
    public Graph graph;
    private Dictionary<long, float> forwardDistances;
    private Dictionary<long, float> backwardDistances;
    private Dictionary<long, long> forwardPrevious;
    private Dictionary<long, long> backwardPrevious;
    private SortedSet<Node> forwardQueue;
    private SortedSet<Node> backwardQueue;
    private long forwardMeetingNode, backwardMeetingNode;
    private float minDistance;
    private long startNode, endNode;

    public BiDijkstra(Graph graph)
    {
        this.graph = graph;
        Reset();
    }

    private void Reset()
    {
        forwardDistances = new Dictionary<long, float>();
        backwardDistances = new Dictionary<long, float>();
        forwardPrevious = new Dictionary<long, long>();
        backwardPrevious = new Dictionary<long, long>();
        forwardQueue = new SortedSet<Node>();
        backwardQueue = new SortedSet<Node>();
        minDistance = float.PositiveInfinity;
        forwardMeetingNode = backwardMeetingNode = -1;
    }

    public PathResult FindShortestPath(long start, long end)
    {
        startNode = start;
        endNode = end;
        Reset();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Initialize both searches
        forwardQueue.Add(new Node(start, 0));
        forwardDistances[start] = 0;
        backwardQueue.Add(new Node(end, 0));
        backwardDistances[end] = 0;

        while (forwardQueue.Count > 0 && backwardQueue.Count > 0)
        {
            // Process forward direction
            if (ProcessQueue(forwardQueue, forwardDistances, backwardDistances, forwardPrevious, ref forwardMeetingNode, ref minDistance, true))
            {
                stopwatch.Stop();
                var path = ConstructPath(forwardMeetingNode, forwardPrevious, backwardPrevious);
                return new PathResult(start, end, minDistance, stopwatch.ElapsedMilliseconds, forwardDistances.Count + backwardDistances.Count, MapController.ReconstructPath(path, start, end));
            }

            // Process backward direction
            if (ProcessQueue(backwardQueue, backwardDistances, forwardDistances, backwardPrevious, ref backwardMeetingNode, ref minDistance, false))
            {
                stopwatch.Stop();
                var path = ConstructPath(backwardMeetingNode, forwardPrevious, backwardPrevious);
                return new PathResult(start, end, minDistance, stopwatch.ElapsedMilliseconds, forwardDistances.Count + backwardDistances.Count, MapController.ReconstructPath(path, start, end));
            }
        }

        stopwatch.Stop();
        return new PathResult(start, end, -1, stopwatch.ElapsedMilliseconds, forwardDistances.Count + backwardDistances.Count, new long[]{});
    }

    private bool ProcessQueue(SortedSet<Node> queue, Dictionary<long, float> distances, Dictionary<long, float> otherDistances, Dictionary<long, long> previous, ref long meetingNode, ref float minDistance, bool isForward)
    {
        // Dequeue the closest node
        var current = queue.Min;
        queue.Remove(current);

        // If the current node's distance is greater than or equal to the minimum known distance, stop
        if (current.Distance >= minDistance)
        {
            return true;
        }

        // Get neighbors based on direction
        var neighbors = isForward ? graph.graph[current.Id] : graph.bi_graph[current.Id];

        // Process all neighbors
        foreach (var edge in neighbors)
        {
            var newDist = current.Distance + edge.cost;

            // If this path is shorter, update the distance and queue
            if (newDist < distances.GetValueOrDefault(edge.node, float.PositiveInfinity))
            {
                distances[edge.node] = newDist;
                previous[edge.node] = current.Id;
                queue.Add(new Node(edge.node, newDist));

                // If the node was reached by the other search, update the minimum distance
                if (otherDistances.ContainsKey(edge.node))
                {
                    var potentialMinDistance = newDist + otherDistances[edge.node];
                    if (potentialMinDistance < minDistance)
                    {
                        minDistance = potentialMinDistance;
                        meetingNode = edge.node;
                    }
                }
            }
        }

        return false;
    }

    private Dictionary<long, long> ConstructPath(long meetingNode, Dictionary<long, long> forwardPrev, Dictionary<long, long> backwardPrev)
    {
        var merged = new Dictionary<long, long>();
        long current = meetingNode;

        while (forwardPrev.ContainsKey(current))
        {
            merged[forwardPrev[current]] = current;
            current = forwardPrev[current];
        }

        current = meetingNode;
        while (backwardPrev.ContainsKey(current))
        {
            merged[current] = backwardPrev[current];
            current = backwardPrev[current];
        }

        return merged;
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        throw new NotImplementedException();
    }

    private class Node : IComparable<Node>
    {
        public long Id { get; }
        public float Distance { get; }

        public Node(long id, float distance)
        {
            Id = id;
            Distance = distance;
        }

        public int CompareTo(Node other)
        {
            var result = Distance.CompareTo(other.Distance);
            return result == 0 ? Id.CompareTo(other.Id) : result;
        }
    }
}
