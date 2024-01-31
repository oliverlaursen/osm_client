using OsmSharp.Streams;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;


[System.Serializable]
public class Node
{
    public long id;
    public double lat;
    public double lon;
}

[System.Serializable]
public class Way
{
    public long id;
    public long[] node_refs;
}

[System.Serializable]
public class PreprocessedOSM
{
    public List<Node> nodes;
    public List<Way> ways;
}

public class PreProcess : MonoBehaviour {
    public static PreprocessedOSM PreProcessMap(TextAsset mapFile){

        HashSet<string> blacklist = new HashSet<string>
        {
            "pedestrian",
            "footway",
            "steps",
            "path",
            "cycleway",
            "proposed",
            "construction",
            "bridleway",
            "abandoned",
            "platform",
            "raceway",
            "service",
            "services",
            "rest_area",
            "escape",
            "raceway",
            "busway",
            "footway",
            "bridlway",
            "steps",
            "corridor",
            "via_ferreta",
            "sidewalk",
            "crossing",
            "proposed",
            "track",
        };

        // Load osm .pbf file
        using (var memStream = new MemoryStream(mapFile.bytes))
        {
            var source = new PBFOsmStreamSource(memStream);

            var nodes = new ConcurrentBag<Node>();
            var ways = new ConcurrentBag<Way>();

            Parallel.ForEach(source, osmGeo =>
           {
               if (osmGeo.Type == OsmSharp.OsmGeoType.Node)
               {
                   nodes.Add(new Node
                   {
                       id = osmGeo.Id.Value,
                       lat = (osmGeo as OsmSharp.Node).Latitude.Value,
                       lon = (osmGeo as OsmSharp.Node).Longitude.Value,
                   });
               }
               else if (osmGeo.Type == OsmSharp.OsmGeoType.Way
                        && osmGeo.Tags.Any(t => t.Key == "highway")
                        && !blacklist.Contains(osmGeo.Tags["highway"]))
               {
                   ways.Add(new Way
                   {
                       id = osmGeo.Id.Value,
                       node_refs = (osmGeo as OsmSharp.Way).Nodes
                   });
               }
           });
            var result = new PreprocessedOSM { nodes = nodes.ToList(), ways = ways.ToList() };
            return result;
        }
    }

    public byte[] SerializeToByteArray(PreprocessedOSM data)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        using (MemoryStream stream = new MemoryStream())
        {
            formatter.Serialize(stream, data);
            return stream.ToArray();
        }
    }

    public void WriteToBinaryFile(string path, PreprocessedOSM result){
        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                formatter.Serialize(stream, result);
            }
    }
}