using System;
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
    public int uses;
}

public class Edge
{
    public long way_id;
    public long source_id;
    public long target_id;
    public Coord[] geometry;
    public long[] node_ids;

    public double length()
    {
        double length = 0;
        for (int i = 0; i < geometry.Length - 1; i++)
        {
            length += geometry[i].distance(geometry[i + 1]);
        }
        return length;
    }

    public double length_until(long nodeId)
    {
        double length = 0;
        for (int i = 0; i < geometry.Length - 1; i++)
        {
            length += geometry[i].distance(geometry[i + 1]);
            if (node_ids[i] == nodeId)
            {
                return length;
            }
        }
        return length;
    }
}