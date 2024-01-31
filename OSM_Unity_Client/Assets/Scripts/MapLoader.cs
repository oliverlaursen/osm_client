using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DotSpatial.Projections;
using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Unity.VisualScripting;


public class Point
{
    public long id;
    public double x;
    public double y;

    public Point(long id, double x, double y)
    {
        this.id = id;
        this.x = x;
        this.y = y;
    }
}

public class MapLoader : MonoBehaviour
{
    private CoordinateTransformationFactory coordinateTransformationFactory;
    private GeographicCoordinateSystem geographicCoordinateSystem;

    void Awake(){
        coordinateTransformationFactory = new CoordinateTransformationFactory();
        geographicCoordinateSystem = GeographicCoordinateSystem.WGS84;
    }

    public Point ConvertLatLonToUTM(Node node)
    {
        var longitude = node.lon;
        var latitude = node.lat;
        int utmZone = (int)Math.Floor((longitude + 180) / 6) + 1;

        // Create UTM Coordinate System
        var utmCoordinateSystem = ProjectedCoordinateSystem.WGS84_UTM(utmZone, latitude >= 0);

        // Create Transformation
        var transformation = coordinateTransformationFactory.CreateFromCoordinateSystems(geographicCoordinateSystem, utmCoordinateSystem);

        // Perform the transformation
        double[] fromPoint = new double[] { longitude, latitude };
        double[] toPoint = transformation.MathTransform.Transform(fromPoint);

        return new Point(node.id, toPoint[0], toPoint[1]);
    }

    public Dictionary<long, (double, double)> ProjectCoordinates(List<Node> nodeList)
    {
        var zero_ref = ConvertLatLonToUTM(nodeList[0]);
        var points = new Dictionary<long, (double, double)>();
        foreach (Node node in nodeList)
        {
            Point point = ConvertLatLonToUTM(node);
            points[point.id] = (point.x - zero_ref.x, point.y - zero_ref.y);
        }
        return points;
    }

    public static void DrawRoads(Dictionary<long, (double, double)> points, List<Way> ways)
    {
        foreach (Way way in ways)
        {
            var nr = way.node_refs;
            for (int i = 0; i < nr.Length - 1; i++)
            {
                var node1Pos = points[nr[i]];
                var node2Pos = points[nr[i+1]];
                Vector3 pos1 = new((float)node1Pos.Item1, (float)node1Pos.Item2, 0);
                Vector3 pos2 = new((float)node2Pos.Item1, (float)node2Pos.Item2, 0);
                Debug.DrawLine(pos1, pos2, Color.red, 10000f);
            }
        }
    }
}


