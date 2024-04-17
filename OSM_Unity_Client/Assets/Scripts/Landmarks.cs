using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Landmarks : IPathfindingAlgorithm
{
    private Graph graph;
    private Dictionary<long, Edge[]> f;
    private Dictionary<long, Edge[]> r;


    public Landmarks(Graph graph)
    {
        this.graph = graph;
        f = graph.graph;
        r = graph.bi_graph;
    }

    public void FindShortestPath(long start, long end)
    {
        // First select two best landmarks (best forward + best reverse)
        List<(long,double)> landmarkInfosForward = new();
        List<(long,double)> landmarkInfosReverse = new();

        foreach (Landmark landmark in graph.landmarks)
        {
            landmarkInfosForward.Add((landmark.node_id, landmark.distances[end]));
            landmarkInfosReverse.Add((landmark.node_id, landmark.distances[start]));
        }
        landmarkInfosForward.Sort((x, y) => x.Item2.CompareTo(y.Item2));
        landmarkInfosReverse.Sort((x, y) => x.Item2.CompareTo(y.Item2));
        long bestForward = landmarkInfosForward[0].Item1;
        long bestReverse = landmarkInfosReverse[0].Item1;

        

        throw new System.NotImplementedException();
    }


    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        throw new System.NotImplementedException();
    }
}