using System.Collections.Generic;
using UnityEngine;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;


public class Point
{
    public double x;
    public double y;

    public Point(double x, double y)
    {
        this.x = x;
        this.y = y;
    }
}

public struct NodeData
{
    public float Latitude;
    public float Longitude;
    public uint Id;

    public NodeData(OsmSharp.Node node)
    {
        Latitude = (float)node.Latitude;
        Longitude = (float)node.Longitude;
        Id = (uint)node.Id;
    }
}

public struct ProjectedData
{
    public float x;
    public float y;
    public uint id;
};

public class MapLoader : MonoBehaviour
{
    public Point GetCenterPoint(IEnumerable<OsmSharp.Node> nodeList)
    {
        double latSum = 0;
        double lonSum = 0;
        int count = 0;
        foreach (OsmSharp.Node node in nodeList)
        {
            latSum += (double)node.Latitude;
            lonSum += (double)node.Longitude;
            count += 1;
        }

        return new Point(latSum / count, lonSum / count);
    }

    public Dictionary<long, (double, double)> ProjectCoordinates(IEnumerable<OsmSharp.Node> nodeList)
    {
        var centerPoint = GetCenterPoint(nodeList);
        var points = new Dictionary<long, (double, double)>();
        foreach (OsmSharp.Node node in nodeList)
        {
            var projected = ProjectToAzimuthalEquidistant(node, centerPoint);
            points.Add((long)node.Id, (projected.x, projected.y));
        }
        return points;
    }

    public static void DrawRoads(Dictionary<long, (double, double)> points, IEnumerable<OsmSharp.Way> ways)
    {
        var notPresent = 0;
        foreach (OsmSharp.Way way in ways)
        {
            var nr = way.Nodes;
            for (int i = 0; i < nr.Length - 1; i++)
            {
                if (!points.ContainsKey(nr[i]) || !points.ContainsKey(nr[i + 1]))
                {
                    notPresent += 1;
                    continue;
                }
                var node1Pos = points[nr[i]];
                var node2Pos = points[nr[i + 1]];
                Vector3 pos1 = new((float)node1Pos.Item1, (float)node1Pos.Item2, 0);
                Vector3 pos2 = new((float)node2Pos.Item1, (float)node2Pos.Item2, 0);
                Debug.DrawLine(pos1, pos2, Color.red, 10000f);
            }
        }
        Debug.Log("Not present: " + notPresent);
    }

    public static Point ProjectToAzimuthalEquidistant(OsmSharp.Node node, Point centerPoint)
    {
        // Convert degrees to radians
        double latRad = (double)node.Latitude * (Math.PI / 180);
        double lonRad = (double)node.Longitude * (Math.PI / 180);
        double centerLatRad = centerPoint.x * (Math.PI / 180);
        double centerLonRad = centerPoint.y * (Math.PI / 180);

        // Earth's radius in meters
        double R = 6371000;

        // Calculate differences
        double deltaLon = lonRad - centerLonRad;

        // Calculate great circle distance (central angle) using the spherical law of cosines
        double centralAngle = Math.Acos(Math.Sin(centerLatRad) * Math.Sin(latRad) +
                                        Math.Cos(centerLatRad) * Math.Cos(latRad) * Math.Cos(deltaLon));

        // Distance from the center of the projection
        double distance = R * centralAngle;

        // Azimuth (bearing) from the center to the point
        double azimuth = Math.Atan2(Math.Sin(deltaLon),
                                    Math.Cos(centerLatRad) * Math.Tan(latRad) -
                                    Math.Sin(centerLatRad) * Math.Cos(deltaLon));

        // Convert polar coordinates (distance, azimuth) to Cartesian (x, y).
        // Note: Azimuthal equidistant projection preserves both distance and direction from the center point.
        double x = distance * Math.Sin(azimuth);
        double y = distance * Math.Cos(azimuth);

        return new Point(x, y);
    }
}


