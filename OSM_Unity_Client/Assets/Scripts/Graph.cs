using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Coord
{
    public double lon;
    public double lat;

    public double distance(Coord other)
    {
        /**
            * Returns the distance between two coordinates using the haversine formula
         */
        double R = 63781000; // Radius of the earth in meters
        double dLat = (other.lat - this.lat) * Math.PI / 180;
        double dLon = (other.lon - this.lon) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(this.lat * Math.PI / 180) * Math.Cos(other.lat * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double d = R * c;
        return d;
    }
}

public class Node
{
    public long id;
    public Coord coord;
}

public class Edge
{
    public long node;
    public int cost;
}

public class Graph
{
    public Dictionary<long,Edge[]> graph;
    public Dictionary<long, float[]> nodes;
    public Dictionary<long, long[]> ways;
}
