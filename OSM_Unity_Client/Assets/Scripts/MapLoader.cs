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

    public Point ConvertLatLonToUTM(Node node, ProjectedCoordinateSystem utmCoordinateSystem)
    {
        // Create Transformation
        var transformation = coordinateTransformationFactory.CreateFromCoordinateSystems(geographicCoordinateSystem, utmCoordinateSystem);

        // Perform the transformation
        double[] fromPoint = new double[] { node.lon, node.lat };
        double[] toPoint = transformation.MathTransform.Transform(fromPoint);

        return new Point(node.id, toPoint[0], toPoint[1]);
    }

    public Dictionary<long, (double, double)> ProjectCoordinates(List<Node> nodeList)
    {
        var firstPoint = nodeList[0];
        int utmZone = (int)((firstPoint.lon + 180) / 6) + 1;
        bool zoneIsNorth = firstPoint.lat >= 0;
        var utmCoordinateSystem = ProjectedCoordinateSystem.WGS84_UTM(utmZone, zoneIsNorth);


        var zeroRef = ConvertLatLonToUTM(firstPoint, utmCoordinateSystem);
        var points = new Dictionary<long, (double, double)>();
        foreach (Node node in nodeList)
        {
            Point point = ConvertLatLonToUTM(node, utmCoordinateSystem);
            points[point.id] = (point.x - zeroRef.x, point.y - zeroRef.y);
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


