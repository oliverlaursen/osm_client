using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

[System.Serializable]
public class Node
{
    public string id;
    public double lat;
    public double lon;
}

public class Point
{
    public string id;
    public double x;
    public double y;

    public Point(string id, double x, double y)
    {
        this.id = id;
        this.x = x;
        this.y = y;
    }
}

[System.Serializable]
public class Way
{
    public string id;
    public long[] node_refs;
}

[System.Serializable]
public class PreprocessedOSM
{
    public List<Node> nodes;
    public List<Way> ways;
}

public class MapLoader : MonoBehaviour
{
    List<Node> nodeList;
    public Point LatLonToPoint(Node node)
    {
        double lat = node.lat;
        double lon = node.lon;

        ICoordinateSystem sourceCS = GeographicCoordinateSystem.WGS84;
        ICoordinateSystem targetCS = ProjectedCoordinateSystem.WebMercator;

        // Create a coordinate transformation
        ICoordinateTransformation transformation = new CoordinateTransformationFactory().CreateFromCoordinateSystems(sourceCS, targetCS);

        // Transform the coordinates
        double[] input = { longitude, latitude };
        double[] output = transformation.MathTransform.Transform(input);

        return new Point(node.id, output[0], output[1]);
    }
    public TextAsset mapFile;



    
}


