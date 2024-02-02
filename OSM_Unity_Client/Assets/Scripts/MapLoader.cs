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
};


public struct ProjectedData
{
    public double x;
    public double y;
    public long id;
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

    public (double, double, double, double) FindBoundingBox(ProjectedData[] points)
    {
        double minX = double.MaxValue;
        double maxX = double.MinValue;
        double minY = double.MaxValue;
        double maxY = double.MinValue;
        foreach (ProjectedData point in points)
        {
            if (point.x < minX)
            {
                minX = point.x;
            }
            if (point.x > maxX)
            {
                maxX = point.x;
            }
            if (point.y < minY)
            {
                minY = point.y;
            }
            if (point.y > maxY)
            {
                maxY = point.y;
            }
        }
        return (minX, maxX, minY, maxY);
    }

    public Dictionary<long, (double, double)> ProjectCoordinates(IEnumerable<OsmSharp.Node> nodeList)
    {
        double targetWidth = 800;
        double targetHeight = 600;
        var centerPoint = GetCenterPoint(nodeList);
        var points = new ProjectedData[nodeList.Count()];
        for (int i = 0; i < nodeList.Count(); i++)
        {
            var node = nodeList.ElementAt(i);
            var projected = ProjectToAzimuthalEquidistant(node, centerPoint);
            points[i] = new ProjectedData { x = projected.x, y = projected.y, id = (long)node.Id };
        }
        var (minX, maxX, minY, maxY) = FindBoundingBox(points);
        double mapWidth = maxX - minX;
        double mapHeight = maxY - minY;

        // Calculate scale factors for both dimensions
        double scaleX = targetWidth / mapWidth;
        double scaleY = targetHeight / mapHeight;

        // Use the smaller scale factor to maintain aspect ratio
        double scaleFactor = Math.Min(scaleX, scaleY);
        var scaledPoints = new Dictionary<long, (double, double)>();
        foreach (var point in points)
        {
            // Scale points
            double scaledX = (point.x - minX) * scaleFactor;
            double scaledY = (point.y - minY) * scaleFactor;

            // Center points within the target dimensions
            double offsetX = (targetWidth - (mapWidth * scaleFactor)) / 2;
            double offsetY = (targetHeight - (mapHeight * scaleFactor)) / 2;

            scaledPoints[point.id] = (scaledX + offsetX, scaledY + offsetY);
        }
        return scaledPoints;
    }

    public static void DrawRoads(Dictionary<long, (double, double)> points, IEnumerable<OsmSharp.Way> ways, GLLineRenderer lineRenderer)
    {
        lineRenderer.ClearLines(); // Clear existing lines

        foreach (OsmSharp.Way way in ways)
        {
            var nr = way.Nodes;
            for (int i = 0; i < nr.Length - 1; i++)
            {
                var node1Pos = points[nr[i]];
                var node2Pos = points[nr[i + 1]];
                Vector3 pos1 = new((float)node1Pos.Item1, (float)node1Pos.Item2, 0);
                Vector3 pos2 = new((float)node2Pos.Item1, (float)node2Pos.Item2, 0);

                // Use GLLineRenderer to add lines
                lineRenderer.AddLine(pos1, pos2);
            }
        }
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


