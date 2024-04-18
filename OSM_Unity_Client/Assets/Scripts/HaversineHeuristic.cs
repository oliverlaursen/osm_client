using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

public class HaversineHeuristic : AStarHeuristic
{
    private Graph graph;
    public HaversineHeuristic(Graph graph)
    {
        this.graph = graph;
    }

    public float Calculate(long start, long end)
    {
        var startCoords = graph.nodes[start];
        double startLat = startCoords.Item2[0]; // Convert to radians
        double startLon = startCoords.Item2[1]; // Convert to radians

        double startLat_radians = startLat * (Math.PI / 180);
        double startLon_radians = startLon * (Math.PI / 180);

        var endCoords = graph.nodes[end];
        double endLat = endCoords.Item2[0]; // Convert to radians
        double endLon = endCoords.Item2[1]; // Convert to radians

        double endLat_radians = endLat * (Math.PI / 180);
        double endLon_radians = endLon * (Math.PI / 180);

        double dLat = endLat_radians - startLat_radians;
        double dLon = endLon_radians - startLon_radians;

        double r = 6371000; //radius of the earth in meters

        double a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2)) +
                   (Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(startLat_radians) * Math.Cos(endLat_radians));
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        double dist = r * c;

        var floatDist = (float)dist;

        Assert.IsTrue(floatDist >= 0);

        return (float)dist;
    }
}