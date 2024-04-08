using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BiDijkstra : IPathfindingAlgorithm
{
    public Graph graph;

    public BiDijkstra(Graph graph)
    {
        this.graph = graph;
    }

    public void FindShortestPath(long start, long end)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int nodesVisited = 0;
        (var distances, var previous, var visited, var queue) = Dijkstra.InitializeDijkstra(start, graph);
        (var distances2, var previous2, var visited2, var queue2) = Dijkstra.InitializeDijkstra(end, graph);

        while (queue.Count > 0 && queue2.Count > 0)
        {
            var (distance, currentNode) = queue.Min;
            queue.Remove(queue.Min);
            if (!visited.Add(currentNode)) continue;

            if (visited2.Contains(currentNode))
            {
                stopwatch.Stop();
                MapController.DisplayStatistics(start, end, distance + distances2[currentNode], stopwatch.ElapsedMilliseconds, nodesVisited);
                var allPrev = MergePrevious(previous, previous2);
                GameObject.Find("Map").GetComponent<MapController>().DrawPath(graph.nodes, MapController.ReconstructPath(allPrev, start, end));
                return;
            }

            Dijkstra.UpdateNeighbors(currentNode, distance, graph.graph[currentNode], ref distances, ref previous, ref queue, ref nodesVisited, visited, graph);

            var (distance2, currentNode2) = queue2.Min;
            queue2.Remove(queue2.Min);
            if (!visited2.Add(currentNode2)) continue;

            if (visited.Contains(currentNode2))
            {
                stopwatch.Stop();
                MapController.DisplayStatistics(start, end, distance2 + distances[currentNode2], stopwatch.ElapsedMilliseconds, nodesVisited);
                var allPrev = MergePrevious(previous, previous2);
                var path = MapController.ReconstructPath(allPrev, start, end);
                Debug.Log("Path: " + string.Join(", ", path));
                GameObject.Find("Map").GetComponent<MapController>().DrawPath(graph.nodes, path);
                return;
            }

            Dijkstra.UpdateNeighbors(currentNode2, distance2, graph.bi_graph[currentNode2], ref distances2, ref previous2, ref queue2, ref nodesVisited, visited2, graph);
        }
    }

    private Dictionary<long, long> MergePrevious(Dictionary<long, long> previous, Dictionary<long, long> previous2)
    {
        Debug.Log("Previous: " + string.Join(", ", previous));

        foreach (var item in previous2)
        {
            if (!previous.ContainsKey(item.Key) && !previous.ContainsValue(-1))
            {
                previous.Add(item.Value, item.Key);
            }
        }
        Debug.Log("Previous: " + string.Join(", ", previous));
        return previous;
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end)
    {
        throw new System.NotImplementedException();
    }
}
