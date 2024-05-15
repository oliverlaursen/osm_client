using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;
using UnityEngine;

public class AStar : IPathfindingAlgorithm
{
    public Graph graph;
    public FastPriorityQueue<PriorityQueueNode> openList;
    private Dictionary<long, PriorityQueueNode> priorityQueueNodes;
    private HashSet<long> openSet;
    public HashSet<long> closedSet;
    public Dictionary<long, long> parent;
    public Dictionary<long, float> gScore;
    public Dictionary<long, float> fScore;
    public AStarHeuristic heuristic;
    private IEnumerable<Landmark> landmarks;
    private int updateLandmarks;
    public int nodesVisited;
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
        //openList = new SimplePriorityQueue<long, float>();
        openList = new FastPriorityQueue<PriorityQueueNode>(graph.nodes.Count);
        priorityQueueNodes = new Dictionary<long, PriorityQueueNode>();
        openSet = new HashSet<long>();
        closedSet = new HashSet<long>();
        parent = new Dictionary<long, long>();
        gScore = new Dictionary<long, float>() { [start] = 0 };
        fScore = new Dictionary<long, float>() { [start] = heuristic.Calculate(start, end) };

        PriorityQueueNode startNode = new PriorityQueueNode(start);
        openList.Enqueue(startNode, fScore[start]);
        priorityQueueNodes[start] = startNode;
        openSet.Add(start);
        nodesVisited = 0;
    }

    public long DequeueAndUpdateSets()
    {
        var node = openList.Dequeue().Id;
        openSet.Remove(node);
        return node;
    }


    public PathResult FindShortestPath(long start, long end)
    {
        InitializeSearch(start, end);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (openList.Count > 0)
        {
            long current = DequeueAndUpdateSets();
            if (current == end){
                stopwatch.Stop();
                return new PathResult(start, end, gScore[end], stopwatch.ElapsedMilliseconds, nodesVisited, MapController.ReconstructPath(parent, start, end));
            }
            nodesVisited++;
            closedSet.Add(current);
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
            var bestLandmarks = Landmarks.FindBestLandmark(landmarks, start, end, 3);
            if (visual)
            {
                Landmarks.MarkLandmarks(landmarks.ToArray(), Color.blue);
                Landmarks.MarkLandmarks(bestLandmarks.Select(x => x.Item1).ToArray(), Color.yellow);
            }
            ChangeHeuristic(new MultLandmarkHeuristic(bestLandmarks.Select(x => new LandmarkHeuristic(x.Item1,x.Item2)).ToArray()));
        }
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        InitializeSearch(start, end);
        var lineRenderer = Camera.main.GetComponent<GLLineRenderer>(); // Ensure Camera has GLLineRenderer component
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();

        while (openList.Count > 0)
        {
            long current = DequeueAndUpdateSets();
            if (current == end)
            {
                stopwatch.Stop();
                lineRenderer.ClearDiscoveryPath();
                var result = new PathResult(start, end, gScore[end], stopwatch.ElapsedMilliseconds, nodesVisited, MapController.ReconstructPath(parent, start, end));
                result.DisplayAndDrawPath(graph);
                yield break;
            }
            nodesVisited++;
            var neighbors = graph.GetNeighbors(current);
            UpdateNeighborsWithVisual(current, end, neighbors, lineRenderer);
            UpdateLandmarks(nodesVisited, current, end, true);
            if (drawspeed == 0) yield return null;
            else if (stopwatch2.ElapsedTicks > drawspeed)
            {
                yield return null;
                stopwatch2.Restart();
            }
        }
    }


    public void UpdateNeighbors(long current, long end, IEnumerable<Edge> neighbors)
    {
        foreach (var neighbor in neighbors)
        {
            nodesVisited++;
            if (closedSet.Contains(neighbor.node)) continue;
            TryEnqueueNeighbor(neighbor, current, end);
        }
    }

    public void UpdateNeighborsWithVisual(long current, long end, IEnumerable<Edge> neighbors, GLLineRenderer lineRenderer)
    {
        foreach (var neighbor in neighbors)
        {
            if (closedSet.Contains(neighbor.node)) continue;
            nodesVisited++;
            var result = TryEnqueueNeighbor(neighbor, current, end);
            if (result)
            {
                var startCoord = graph.nodes[current];
                var endCoord = graph.nodes[neighbor.node];
                lineRenderer.AddDiscoveryPath(new List<Vector3> { new(startCoord.Item1[0], startCoord.Item1[1], 0), new(endCoord.Item1[0], endCoord.Item1[1], 0) });
            }
        }
    }

    private bool TryEnqueueNeighbor(Edge neighbor, long current, long end)
    {
        var tentativeGScore = gScore[current] + neighbor.cost;
        if (!gScore.ContainsKey(neighbor.node) || tentativeGScore < gScore[neighbor.node])
        {
            parent[neighbor.node] = current;
            gScore[neighbor.node] = tentativeGScore;
            fScore[neighbor.node] = tentativeGScore + heuristic.Calculate(neighbor.node, end);
            PriorityQueueNode neighborNode = new PriorityQueueNode(neighbor.node);
            if (!openSet.Contains(neighbor.node))
            {
                openList.Enqueue(neighborNode, fScore[neighbor.node]);
                priorityQueueNodes[neighbor.node] = neighborNode;
                openSet.Add(neighbor.node);
            }
            else
            {
                PriorityQueueNode nodeToUpdate = priorityQueueNodes[neighbor.node];
                openList.UpdatePriority(nodeToUpdate, fScore[neighbor.node]);
            }
            return true;
        }
        return false;
    }
}