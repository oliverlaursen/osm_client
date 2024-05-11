using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;

public class Landmarks : IPathfindingAlgorithm
{
    private Graph graph;
    private AStar astar;
    private bool showLandmarks;

    public Landmarks(Graph graph, bool showLandmarks = true, int updateLandmarks = 0)
    {
        this.graph = graph;
        this.astar = new AStar(graph, graph.landmarks, updateLandmarks);
        this.showLandmarks = showLandmarks;
    }

    public static (Landmark, bool)[] FindBestLandmark(IEnumerable<Landmark> landmarks, long start, long end, int n)
    {
        List<(Landmark, float, bool)> lowerBounds = new();

        foreach (Landmark landmark in landmarks)
        {
            var c_behind = landmark.distances[end] - landmark.distances[start];
            lowerBounds.Add((landmark, c_behind, true));
            var c_ahead = landmark.bi_distances[start] - landmark.bi_distances[end];
            lowerBounds.Add((landmark, c_ahead, false));
        }
        lowerBounds.Sort((x, y) => x.Item2.CompareTo(y.Item2));
        lowerBounds.Reverse();
        var bestLandmarks = lowerBounds.Select(x => (x.Item1, x.Item3)).Take(n).ToArray();
        return bestLandmarks;
    }

    public PathResult FindShortestPath(long start, long end)
    {
        // First select the 3 best landmark (landmarks with highest lower bound in triangle inequality)
        var bestLandmarks = FindBestLandmark(graph.landmarks, start, end, 3);
        if (showLandmarks)
        {
            MarkLandmarks(graph.landmarks.ToArray(), Color.blue);
            MarkLandmarks(bestLandmarks.Select(x => x.Item1).ToArray(), Color.yellow);
        }
        astar.ChangeHeuristic(new MultLandmarkHeuristic(bestLandmarks.Select(x => new LandmarkHeuristic(x.Item1, x.Item2)).ToArray()));
        return astar.FindShortestPath(start, end);
    }

    public static void MarkLandmark(Landmark landmark, Color color)
    {
        GameObject.Find("Landmark " + landmark.node_id).GetComponent<SpriteRenderer>().color = color;
    }

    public static void MarkLandmarks(Landmark[] landmarks, Color color)
    {
        foreach (Landmark landmark in landmarks)
        {
            MarkLandmark(landmark, color);
        }
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        var bestLandmarks = FindBestLandmark(graph.landmarks, start, end, 3);
        if (showLandmarks)
        {
            MarkLandmarks(graph.landmarks.ToArray(), Color.blue);
            MarkLandmarks(bestLandmarks.Select(x => x.Item1).ToArray(), Color.yellow);
        }
        astar.ChangeHeuristic(new MultLandmarkHeuristic(bestLandmarks.Select(x => new LandmarkHeuristic(x.Item1, x.Item2)).ToArray()));
        return astar.FindShortestPathWithVisual(start, end, drawspeed);
    }
}