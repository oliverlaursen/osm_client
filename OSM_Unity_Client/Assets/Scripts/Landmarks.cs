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

    public Landmarks(Graph graph)
    {
        this.graph = graph;
        this.astar = new AStar(graph, graph.landmarks);
    }

    public static Landmark[] FindBestLandmark(IEnumerable<Landmark> landmarks, long start, long end, int n)
    {
        List<(Landmark, float)> lowerBounds = new();

        foreach (Landmark landmark in landmarks)
        {
            var a = landmark.distances[start];
            var b = landmark.distances[end];
            var c = a - b;
            lowerBounds.Add((landmark, c));
        }
        lowerBounds.Sort((x, y) => x.Item2.CompareTo(y.Item2));
        Landmark[] bestLandmarks = lowerBounds.Select(x => x.Item1).Take(n).ToArray();
        return bestLandmarks;
    }

    public void FindShortestPath(long start, long end)
    {
        // First select the 3 best landmark (landmarks with highest lower bound in triangle inequality)
        Landmark[] bestLandmarks = FindBestLandmark(graph.landmarks, start, end, 3);
        MarkLandmarks(graph.landmarks.ToArray(), Color.blue);
        MarkLandmarks(bestLandmarks, Color.yellow);
        astar.ChangeHeuristic(new MultLandmarkHeuristic(bestLandmarks.Select(x => new LandmarkHeuristic(x)).ToArray()));
        astar.FindShortestPath(start, end);
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
        Landmark[] bestLandmarks = FindBestLandmark(graph.landmarks, start, end, 3);
        MarkLandmarks(graph.landmarks.ToArray(), Color.blue);
        MarkLandmarks(bestLandmarks, Color.yellow);
        astar.ChangeHeuristic(new MultLandmarkHeuristic(bestLandmarks.Select(x => new LandmarkHeuristic(x)).ToArray()));
        return astar.FindShortestPathWithVisual(start, end, drawspeed);
    }
}