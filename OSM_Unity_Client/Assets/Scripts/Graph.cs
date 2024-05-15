using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using MessagePack;

public struct Coord
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

public struct Node
{
    public long id;
    public Coord coord;
}

[MessagePackObject]
public struct Edge
{
    [Key(0)]
    public long node { get; set; }
    [Key(1)]
    public float cost { get; set; }
}

public class Graph
{
    public Edge[][] graph { get; set; }
    public Edge[][] bi_graph { get; set; }
    public (float[], double[])[] nodes { get; set; }
    public List<Landmark> landmarks { get; set; }
    public Edge[] GetNeighbors(long node)
    {
        if (node < graph.Length && graph[node] != null)
        {
            return graph[node];
        }
        else
        {
            // Log a warning message to help with debugging
            Debug.LogWarning("Node " + node + " does not exist in the graph");
            return new Edge[0];
        }
    }
}

[MessagePackObject]
public class GraphReadFormat
{
    [Key(0)]
    public NodeReadFormat[] nodes { get; set; }
    [Key(1)]
    public List<Landmark> landmarks { get; set; }
}

[MessagePackObject]
public class Landmark{
    [Key(0)]
    public long node_id { get; set; }
    [Key(1)]
    public float[] distances { get; set; }
    [Key(2)]
    public float[] bi_distances { get; set; }

}

[MessagePackObject]
public class NodeReadFormat
{
    [Key(0)]
    public long id { get; set; }
    [Key(1)]
    public float x { get; set; }
    [Key(2)]
    public float y { get; set; }
    [Key(3)]
    public double lat { get; set; }
    [Key(4)]
    public double lon { get; set; }
    [Key(5)]
    public Edge[] neighbours { get; set; }
    [Key(6)]
    public Edge[] bi_neighbours { get; set; }
}