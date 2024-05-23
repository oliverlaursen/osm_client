using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;
using UnityEngine;

public class AStar : IPathfindingAlgorithm
{
    private Graph graph;
    public FastPriorityQueue<PriorityQueueNode> queue;
    private Dictionary<long, PriorityQueueNode> priorityQueueNodes;
    public Dictionary<long, long> previous;
    public Dictionary<long, float> gScore;
    public AStarHeuristic heuristic;
    private IEnumerable<Landmark> landmarks;
    private int updateLandmarks;
    public int nodesVisited;

    private const int DefaultBestLandmarkCount = 3;

    public AStar(Graph graph, IEnumerable<Landmark> landmarks = null, int updateLandmarks = 0)
    {
        this.graph = graph;
        this.heuristic = new HaversineHeuristic(graph);
        this.landmarks = landmarks;
        this.updateLandmarks = updateLandmarks;
    }

    public void ChangeHeuristic(AStarHeuristic heuristic)
    {
        this.heuristic = heuristic;
    }

    public void InitializeSearch(long start, long end)
    {
        queue = new FastPriorityQueue<PriorityQueueNode>(graph.nodes.Length);
        priorityQueueNodes = new Dictionary<long, PriorityQueueNode>();
        previous = new Dictionary<long, long>();
        gScore = new Dictionary<long, float>() { [start] = 0 };

        PriorityQueueNode startNode = new PriorityQueueNode(start);
        queue.Enqueue(startNode, heuristic.Calculate(start, end));
        priorityQueueNodes[start] = startNode;
        nodesVisited = 0;
    }

    public PathResult FindShortestPath(long start, long end)
    {
        InitializeSearch(start, end);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (queue.Count > 0)
        {
            long current = queue.Dequeue().Id;

            if (current == end)
            {
                stopwatch.Stop();
                return new PathResult(start, end, gScore[end], stopwatch.ElapsedMilliseconds, nodesVisited, MapController.ReconstructPath(previous, start, end));
            }
            var neighbors = graph.GetNeighbors(current);
            UpdateNeighbors(current, end, neighbors);
            UpdateLandmarks(nodesVisited, current, end);
        }
        return null;
    }

    private void UpdateLandmarks(int nodesVisited, long start, long end, bool visual = false)
    {
        if (landmarks == null || updateLandmarks == 0) return;
        if (nodesVisited % updateLandmarks == 0)
        {
            var bestLandmarks = Landmarks.FindBestLandmark(landmarks, start, end, DefaultBestLandmarkCount);
            if (visual)
            {
                Landmarks.MarkLandmarks(landmarks.ToArray(), Color.blue);
                Landmarks.MarkLandmarks(bestLandmarks.Select(x => x.Item1).ToArray(), Color.yellow);
            }
            ChangeHeuristic(new MultLandmarkHeuristic(bestLandmarks.Select(x => new LandmarkHeuristic(x.Item1, x.Item2)).ToArray()));
        }
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        InitializeSearch(start, end);
        var lineRenderer = Camera.main.GetComponent<GLLineRenderer>(); // Ensure Camera has GLLineRenderer component
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();

        while (queue.Count > 0)
        {
            long current = queue.Dequeue().Id;
            if (current == end)
            {
                stopwatch.Stop();
                lineRenderer.ClearDiscoveryPath();
                var result = new PathResult(start, end, gScore[end], stopwatch.ElapsedMilliseconds, nodesVisited, MapController.ReconstructPath(previous, start, end));
                result.DisplayAndDrawPath(graph);
                yield break;
            }
            var neighbors = graph.GetNeighbors(current);
            UpdateNeighbors(current, end, neighbors, lineRenderer);
            UpdateLandmarks(nodesVisited, current, end, true);
            if (drawspeed == 0) yield return null;
            else if (stopwatch2.ElapsedTicks > drawspeed)
            {
                yield return null;
                stopwatch2.Restart();
            }
        }
    }

    public void UpdateNeighbors(long current, long end, IEnumerable<Edge> neighbors, GLLineRenderer lineRenderer = null)
    {
        foreach (var neighbor in neighbors)
        {
            var result = TryEnqueueNeighbor(neighbor, current, end);
            if (lineRenderer != null && result)
            {
                var startCoord = graph.nodes[current];
                var endCoord = graph.nodes[neighbor.node];
                lineRenderer.AddDiscoveryPath(new List<Vector3> { new Vector3(startCoord.Item1[0], startCoord.Item1[1], 0), new Vector3(endCoord.Item1[0], endCoord.Item1[1], 0) });
            }
        }
    }

    private bool TryEnqueueNeighbor(Edge edge, long current, long end)
    {
        var neighbor = edge.node;
        var tentativeGScore = gScore[current] + edge.cost;
        if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
        {
            previous[neighbor] = current;
            gScore[neighbor] = tentativeGScore;
            var fscore = tentativeGScore + heuristic.Calculate(neighbor, end);
            PriorityQueueNode neighborNode = new(neighbor);

                // If the node has NEVER been to the queue, add it
                if (!priorityQueueNodes.ContainsKey(neighbor))      
                {
                    nodesVisited++;
                    queue.Enqueue(neighborNode, fscore);
                    priorityQueueNodes[neighbor] = neighborNode;
                }
                // If the node is already in the queue, update its priority
                else if (queue.Contains(priorityQueueNodes[neighbor]))  
                {
                    PriorityQueueNode nodeToUpdate = priorityQueueNodes[neighbor];
                    queue.UpdatePriority(nodeToUpdate, fscore);
                }
                // If the node has been in the queue before but is not in the queue now, add it
                else {                                                  
                    nodesVisited++;
                    queue.Enqueue(priorityQueueNodes[neighbor], fscore);
                }
            return true;
        }
        return false;
    }
}
