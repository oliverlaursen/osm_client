﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using MessagePack;

public class Coord
{
    public float lon;
    public float lat;

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
    public long node {get; set;}
    public int cost {get; set;}
}

public class Graph
{
    public Dictionary<long,Edge[]> graph {get; set;}
    public Dictionary<long, float[]> nodes {get; set;}
}

[MessagePackObject]
public class GraphReadFormat {
    [Key(0)]
    public NodeReadFormat[] nodes {get; set;}
}

[MessagePackObject]
public class NodeReadFormat {
    [Key(0)]
    public long id {get; set;}
    [Key(1)]
    public float x {get; set;}
    [Key(2)]
    public float y {get; set;}
    [Key(3)]
    public (long, int)[] neighbours {get; set;}
    [Key(4)]
    public (long, int)[] bi_neighbours { get; set; }
}