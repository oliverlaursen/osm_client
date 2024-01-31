using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;


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
    public static Point LatLonToPoint(Node node)
    {
        /*double lat = node.lat;
        double lon = node.lon;

        ICoordinateSystem sourceCS = GeographicCoordinateSystem.WGS84;
        ICoordinateSystem targetCS = ProjectedCoordinateSystem.WebMercator;

        // Create a coordinate transformation
        ICoordinateTransformation transformation = new CoordinateTransformationFactory().CreateFromCoordinateSystems(sourceCS, targetCS);

        // Transform the coordinates
        double[] input = { longitude, latitude };
        double[] output = transformation.MathTransform.Transform(input);

        return new Point(node.id, output[0], output[1]);*/
        return new Point(0, 0, 0);
    }

    public static Dictionary<long, (double, double)> ProjectCoordinates(List<Node> nodeList)
    {
        var points = new Dictionary<long, (double, double)>();
        foreach (Node node in nodeList)
        {
            Point point = LatLonToPoint(node);
            points[point.id] = (point.x, point.y);
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
                var node2Pos = points[nr[i + 1]];
                Vector3 pos1 = new Vector3((float)node1Pos.Item1, (float)node1Pos.Item2, 0);
                Vector3 pos2 = new Vector3((float)node2Pos.Item1, (float)node2Pos.Item2, 0);
                Debug.DrawLine(pos1, pos2, Color.red, 10000f);

            }
        }
    }
}


