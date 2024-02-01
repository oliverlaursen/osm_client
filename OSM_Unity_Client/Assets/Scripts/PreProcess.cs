using OsmSharp.Streams;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

[System.Serializable]
public class PreprocessedOSM
{
    public HashSet<OsmSharp.Node> nodes;
    public HashSet<OsmSharp.Way> ways;
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

        // Load osm .pbf file
        using (var memStream = new MemoryStream(mapFile.bytes))
        {
            var source = new PBFOsmStreamSource(memStream);

            var filteredNodes = from osmGeo in source
                                where
                                osmGeo.Type == OsmSharp.OsmGeoType.Node
                                select osmGeo as OsmSharp.Node;

            var filteredWays = from osmGeo in source
                               where
                               (osmGeo.Type == OsmSharp.OsmGeoType.Way
                               && !blacklist.Contains(osmGeo.Tags["highway"]))

                               select osmGeo as OsmSharp.Way;
            var ways = filteredWays.ToHashSet();
            var nodes = filteredNodes.ToHashSet();
            return new PreprocessedOSM { ways = ways, nodes = nodes };
        }
    }
}