using System;
using System.Collections.Generic;
using UnityEngine;

public class AStar
{
    public Graph graph;

    public AStar(Graph graph)
    {
        this.graph = graph;
    }

    public (float, long[]) FindShortestPath(long start, long end)
    {
        Debug.Log("A*");
        //initialize data structures
        SortedSet<Tuple<float, long>> openList = new SortedSet<Tuple<float, long>>(new OpenListComparer());
        HashSet<long> openSet = new HashSet<long>();
        Dictionary<long, long> cameFrom = new Dictionary<long, long>();
        Dictionary<long, float> gScore = new Dictionary<long, float>();
        Dictionary<long, float> fScore = new Dictionary<long, float>();

        int nodesVisited = 0;

        gScore[start] = 0;
        fScore[start] = HeuristicCostEstimate(start, end);
        cameFrom[start] = -1;
        openList.Add(Tuple.Create(fScore[start], start));
        openSet.Add(start);

        while (openList.Count > 0)
        {
            long current = openList.Min.Item2;
            

            if (current == end)
            {
                Debug.Log("Nodes visited: " + nodesVisited);
                return (gScore[current], ReconstructPath(cameFrom, start, end));
            }
            openSet.Remove(current);
            openList.Remove(openList.Min);
            nodesVisited++;

            Edge[] neighbors = graph.GetNeighbors(current);
            foreach (Edge neighbor in neighbors)
            {
                float tentativeGScore = gScore[current] + neighbor.cost;
                if (!gScore.ContainsKey(neighbor.node) || tentativeGScore < gScore[neighbor.node])
                {
                    cameFrom[neighbor.node] = current;
                    gScore[neighbor.node] = tentativeGScore;
                    fScore[neighbor.node] = gScore[neighbor.node] + HeuristicCostEstimate(neighbor.node, end);
                    Tuple<float, long> neighborTuple = new Tuple<float, long>(fScore[neighbor.node], neighbor.node);
                    if (!openSet.Contains(neighbor.node))
                    {
                        openList.Add(neighborTuple);
                        openSet.Add(neighbor.node);
                    }
                    else
                    {
                        // If the node is already in the openList, remove the old tuple and add the new one.
                        openList.RemoveWhere(tuple => tuple.Item2 == neighbor.node);
                        openList.Add(neighborTuple);
                        openSet.Add(neighbor.node);
                    }
                }
            }
        }

        return (0, new long[0]);
    }

    public class OpenListComparer : IComparer<Tuple<float, long>>
    {
        public int Compare(Tuple<float, long> x, Tuple<float, long> y)
        {
            int result = x.Item1.CompareTo(y.Item1);
            if (result == 0)
            {
                result = x.Item2.CompareTo(y.Item2); // Compare by node if fscores are equal
            }
            return result;
        }
    }

    private float HeuristicCostEstimate(long start, long end)
    {
        var startCoords = graph.nodes[start];
        double startLat = startCoords[2]; // Convert to radians
        double startLon = startCoords[3]; // Convert to radians

        double startLat_radians = startLat * (Math.PI / 180);
        double startLon_radians = startLon * (Math.PI / 180);

        var endCoords = graph.nodes[end];
        double endLat = endCoords[2]; // Convert to radians
        double endLon = endCoords[3]; // Convert to radians

        double endLat_radians = endLat * (Math.PI / 180);
        double endLon_radians = endLon * (Math.PI / 180);

        double dLat = endLat_radians - startLat_radians;
        double dLon = endLon_radians - startLon_radians;

        double r = 6371000; //radius of the earth in meters

        double a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2)) +
                   (Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(startLat_radians) * Math.Cos(endLat_radians));
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        double dist = r * c;

        return (float)dist;
    }

    private long[] ReconstructPath(Dictionary<long, long> cameFrom, long start, long end)
    {
        var path = new List<long>();
        long parent = end;
        while (parent != -1)
        {
            path.Add(parent);
            if (!cameFrom.ContainsKey(parent))
            {
                throw new Exception("the end is not reachable from the start node");
            }
            parent = cameFrom[parent];
        }

        path.Reverse();
        return path.ToArray();
    }
}