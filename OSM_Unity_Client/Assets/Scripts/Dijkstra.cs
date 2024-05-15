using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

public class Dijkstra : IPathfindingAlgorithm
{
    public Graph graph;
    public FastPriorityQueue<PriorityQueueNode> queue;
    private Dictionary<long, PriorityQueueNode> priorityQueueNodes;
    public Dictionary<long, float> distances;
    public Dictionary<long, long> previous;
    public HashSet<long> visited;
    public int nodesVisited = 0;
    public Dictionary<(long, long), float> pairDistances = new Dictionary<(long, long), float>();

    public Dijkstra(Graph graph)
    {
        this.graph = graph;

    }

    public void InitializeSearch(long start, Graph graph)
    {
        queue = new FastPriorityQueue<PriorityQueueNode>(graph.nodes.Length);
        priorityQueueNodes = new Dictionary<long, PriorityQueueNode>();
        distances = new Dictionary<long, float>();
        previous = new Dictionary<long, long>();
        visited = new HashSet<long>();

        PriorityQueueNode startNode = new PriorityQueueNode(start);
        queue.Enqueue(startNode, float.MaxValue);
        priorityQueueNodes[start] = startNode;
        distances[start] = 0;
        nodesVisited = 0;
    }


    public PathResult FindShortestPath(long start, long end)
    {
        InitializeSearch(start, graph);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue().Id;
            var distance = distances[currentNode];
            if (!visited.Add(currentNode)) continue;

            if (currentNode == end)
            {
                stopwatch.Stop();
                return new PathResult(start, end, distance, stopwatch.ElapsedMilliseconds, nodesVisited, MapController.ReconstructPath(previous, start, end));
            }

            var neighbors = graph.graph[currentNode];
            UpdateNeighbors(currentNode, distance, neighbors);
        }

        return null;
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        InitializeSearch(start, graph);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
        var lineRenderer = Camera.main.GetComponent<GLLineRenderer>(); // Ensure Camera has GLLineRenderer component

        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue().Id;
            var distance = distances[currentNode];
            if (!visited.Add(currentNode)) continue;

            if (currentNode == end)
            {
                stopwatch.Stop();
                lineRenderer.ClearDiscoveryPath();
                var result = new PathResult(start, end, distance, stopwatch.ElapsedMilliseconds, nodesVisited, MapController.ReconstructPath(previous, start, end));
                result.DisplayAndDrawPath(graph);
                yield break;
            }

            var neighbors = graph.graph[currentNode];
            UpdateNeighbors(currentNode, distance, neighbors, lineRenderer);
            if (drawspeed == 0) yield return null;
                else if (stopwatch2.ElapsedTicks > drawspeed)
                {
                    yield return null;
                    stopwatch2.Restart();
                }
        }
    }

    public void UpdateNeighbors(long currentNode, float distance, IEnumerable<Edge> neighbors, GLLineRenderer lineRenderer = null)
    {
        foreach (var edge in neighbors)
        {
            nodesVisited++;
            var neighbor = edge.node;
            var newDistance = distance + edge.cost;
            if (!distances.ContainsKey(neighbor) || newDistance < distances[neighbor])
            {
                pairDistances[(currentNode, neighbor)] = distance;
                distances[neighbor] = newDistance;
                previous[neighbor] = currentNode;
                PriorityQueueNode neighborNode = new(neighbor);
                if (!queue.Contains(neighborNode))
                {
                    queue.Enqueue(neighborNode, newDistance);
                    priorityQueueNodes[neighbor] = neighborNode;
                }
                else
                {
                    PriorityQueueNode nodeToUpdate = priorityQueueNodes[neighbor];
                    queue.UpdatePriority(nodeToUpdate, newDistance);
                }

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
