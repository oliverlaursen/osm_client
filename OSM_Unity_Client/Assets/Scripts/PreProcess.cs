using OsmSharp.Streams;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

[System.Serializable]
public class PreprocessedOSM
{
    public List<OsmSharp.Node> nodes;
    public List<OsmSharp.Way> ways;
}

public class PreProcess : MonoBehaviour
{
    public static PreprocessedOSM PreProcessMap(TextAsset mapFile)
    {
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

        // Use a dictionary to keep track of node usage
        Dictionary<long, OsmSharp.Node> nodeDictionary = new Dictionary<long, OsmSharp.Node>();
        List<OsmSharp.Way> filteredWays = new List<OsmSharp.Way>();

        using (var memStream = new MemoryStream(mapFile.bytes))
        {
            var source = new PBFOsmStreamSource(memStream).ToList(); // Convert to list once to avoid multiple enumerations

            // First pass: Process ways and record node IDs
            foreach (var osmGeo in source)
            {
                if (osmGeo.Type == OsmSharp.OsmGeoType.Way && osmGeo.Tags.TryGetValue("highway", out var highway) && !blacklist.Contains(highway))
                {
                    filteredWays.Add((OsmSharp.Way)osmGeo);
                    foreach (var nodeId in ((OsmSharp.Way)osmGeo).Nodes)
                    {
                        // Just mark the node as used, to avoid storing duplicates
                        if (!nodeDictionary.ContainsKey(nodeId))
                        {
                            nodeDictionary[nodeId] = null; // Placeholder, actual node will be added in second pass
                        }
                    }
                }
            }

            // Second pass: Process nodes
            foreach (var osmGeo in source)
            {
                if (osmGeo.Type == OsmSharp.OsmGeoType.Node && nodeDictionary.ContainsKey(osmGeo.Id.Value))
                {
                    nodeDictionary[osmGeo.Id.Value] = (OsmSharp.Node)osmGeo; // Replace placeholder with actual node
                }
            }
        }

        // Filter out null entries if any node was not found (shouldn't happen, but just to be safe)
        var filteredNodes = nodeDictionary.Values.Where(node => node != null).ToList();

        return new PreprocessedOSM { ways = filteredWays, nodes = filteredNodes };
    }
}
