using System.Collections.Generic;
using UnityEngine;

public class LandmarkHeuristic : AStarHeuristic
{
    private readonly float[] distances;
    private bool behind;

    public LandmarkHeuristic(Landmark landmark, bool behind)
    {
        distances = behind ? landmark.distances : landmark.bi_distances;
        this.behind = behind;
    }

    public float Calculate(long start, long end)
    {
        var value = behind ? (distances[end] - distances[start]) : (distances[start] - distances[end]);
        return value;
    }
}