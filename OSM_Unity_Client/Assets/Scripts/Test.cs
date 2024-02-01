/*// Pseudocode outline - specific implementation details will vary
using OsmSharp;
using OsmSharp.IO.PBF;
using DotSpatial.Projections;
using OsmSharp.Streams;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using OsmSharp.API;

// 1. Load OSM PBF data
//var source = new PBFOsmStreamSource(new FileStream("path_to_your_file.osm.pbf", FileMode.Open, FileAccess.Read));

// 2. Filter for roads
//var filtered = from osmGeo in source where osmGeo.Type == OsmGeoType.Way && IsRoad(osmGeo) select osmGeo;

// 3. Project coordinates
//ProjectionInfo fromProj = KnownCoordinateSystems.Geographic.World.WGS1984;
//ProjectionInfo toProj = KnownCoordinateSystems.Projected.World.WebMercator; // Choose as needed
//var projected = ProjectCoordinates(filtered, fromProj, toProj);

static List<OsmSharp.Node> ProjectCoordinates(List<OsmSharp.Node> osmGeos, string fromProjString, string toProjString)
{
    // Initialize projection objects
    var fromProj = ProjectionInfo.FromEsriString(fromProjString);
    var toProj = ProjectionInfo.FromEsriString(toProjString);

    foreach (var osmGeo in osmGeos)
    {

        // Convert latitude and longitude to x and y for projection
        double[] points = new double[] { (double)osmGeo.Longitude, (double)osmGeo.Latitude };
        // Z can be optional, depends on your data
        double[] z = new double[] { 0 };

        // Reproject the coordinates
        Reproject.ReprojectPoints(points, z, fromProj, toProj, 0, 1);

        // Update the coordinates with projected values
        osmGeo.Longitude = points[0];
        osmGeo.Latitude = points[1];
    }

    return osmGeos;
}

static bool IsRoad(OsmGeo osmGeo)
{
    var blacklist = new HashSet<string>
        {
            "pedestrian", "footway", "steps", "path", "cycleway",
            "proposed", "construction", "bridleway", "abandoned",
            "platform", "raceway", "service", "services", "rest_area",
            "escape", "busway", "corridor", "via_ferreta",
            "sidewalk", "crossing", "track"
        };
    var highway = osmGeo.Tags["highway"];
    return highway == null || !blacklist.Contains(highway);
}*/