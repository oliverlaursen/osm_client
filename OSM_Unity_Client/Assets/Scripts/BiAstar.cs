using System.Collections;
using UnityEngine;

public class BiAStar : IPathfindingAlgorithm
{
    private Graph graph;
    private AStar forwardAStar;
    private AStar backwardAStar;
    private long meetingNode;
    private float minDistance;

    public BiAStar(Graph graph)
    {
        this.graph = graph;
        forwardAStar = new AStar(graph);
        backwardAStar = new AStar(graph);
    }

    public void Initialize(long start, long end)
    {
        forwardAStar.InitializeSearch(start, end);
        backwardAStar.InitializeSearch(end, start);
        minDistance = float.PositiveInfinity;
        meetingNode = -1;
    }


    public PathResult FindShortestPath(long start, long end)
    {
        Initialize(start, end);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var beta = 0.5 * forwardAStar.heuristic.Calculate(start, end);

        while (forwardAStar.queue.Count > 0 && backwardAStar.queue.Count > 0)
        {
            var topf = forwardAStar.queue.First.Id;
            var topr = backwardAStar.queue.First.Id;

            if (forwardAStar.gScore[topf] + backwardAStar.gScore[topr] >= minDistance + beta)
            {
                stopwatch.Stop();
                var allPrev = BiDijkstra.MergePrevious(forwardAStar.previous, backwardAStar.previous, meetingNode);
                var path = MapController.ReconstructPath(allPrev, start, end);
                var distance = BiDijkstra.ComputeDistance(path, graph);
                return new PathResult(start, end, distance, stopwatch.ElapsedMilliseconds, forwardAStar.nodesVisited + backwardAStar.nodesVisited, path);
            }

            // Process forward direction
            ProcessQueue(forwardAStar, backwardAStar, true, end);

            // Process backward direction
            ProcessQueue(backwardAStar, forwardAStar, false, start);
        }
        return null; // No path found
    }

    private void ProcessQueue(AStar activeAstar, AStar otherAstar, bool isForward, long end, GLLineRenderer lineRenderer = null)
    {
        // Dequeue the closest node
        var currentNode = activeAstar.queue.Dequeue().Id;

        // Get neighbors based on direction
        var neighbors = isForward ? graph.graph[currentNode] : graph.bi_graph[currentNode];

        // Process all neighbors
        activeAstar.UpdateNeighbors(currentNode, end, neighbors, lineRenderer);

        foreach (var edge in neighbors)
        {
            var neighbor = edge.node;
            if (otherAstar.gScore.ContainsKey(neighbor))
            {
                var potentialMinDistance = activeAstar.gScore[neighbor] + otherAstar.gScore[neighbor];
                if (potentialMinDistance < minDistance)
                {
                    minDistance = potentialMinDistance;
                    meetingNode = neighbor;
                }
            }
        }
    }


    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        Initialize(start, end);
        var beta = 0.5 * forwardAStar.heuristic.Calculate(start,end);
        var lineRenderer = Camera.main.gameObject.GetComponent<GLLineRenderer>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();

        while (forwardAStar.queue.Count > 0 && backwardAStar.queue.Count > 0)
        {
            var topf = forwardAStar.queue.First;
            var topr = backwardAStar.queue.First;

            if (forwardAStar.gScore[topf.Id] + backwardAStar.gScore[topr.Id] >= minDistance + beta)
            {
                stopwatch.Stop();
                //lineRenderer.ClearDiscoveryPath();
                var allPrev = BiDijkstra.MergePrevious(forwardAStar.previous, backwardAStar.previous, meetingNode);
                var path = MapController.ReconstructPath(allPrev, start, end);
                var distance = BiDijkstra.ComputeDistance(path, graph);
                var result = new PathResult(start, end, distance, stopwatch.ElapsedMilliseconds, forwardAStar.nodesVisited + backwardAStar.nodesVisited, path);
                result.DisplayAndDrawPath(graph);
                yield break;
            }
            // Process forward direction
            ProcessQueue(forwardAStar, backwardAStar, true, end, lineRenderer);

            // Process backward direction
            ProcessQueue(backwardAStar, forwardAStar, false, start, lineRenderer);

            // Drawing at intervals
            if (drawspeed == 0) yield return null;
            else if (stopwatch2.ElapsedTicks > drawspeed)
            {
                yield return null;
                stopwatch2.Restart();
            }

        }
        yield return null; // No path found
    }
}
