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


public class PreProcess : MonoBehaviour {
    public TextAsset mapFile;
    public void PreProcessMap(){

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
            "proposed"
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
                       id = osmGeo.Id.Value.ToString(),
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
                       id = osmGeo.Id.Value.ToString(),
                       node_refs = (osmGeo as OsmSharp.Way).Nodes
                   });
               }
           });
            var result = new PreprocessedOSM { nodes = nodes.ToList(), ways = ways.ToList() };
            // Save the filtered data to a binary file
            //var path = String.Format("Assets/maps/{0}.pre", mapFile.name);
            //WriteToBinaryFile(path, result);
        
        }
    }

    public void WriteToBinaryFile(string path, PreprocessedOSM result){
        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                formatter.Serialize(stream, result);
            }
    }

    void Start(){
        PreProcessMap();
    }
}