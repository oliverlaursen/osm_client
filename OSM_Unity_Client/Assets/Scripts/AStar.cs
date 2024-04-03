using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using Priority_Queue;
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
        UnityEngine.Debug.Log("A*");
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        //initialize data structures
        SimplePriorityQueue<long> openList = new SimplePriorityQueue<long>();
        HashSet<long> openSet = new HashSet<long>();
        HashSet<long> closedSet = new HashSet<long>();
        Dictionary<long, long> parent = new Dictionary<long, long>();
        Dictionary<long, float> gScore = new Dictionary<long, float>();
        Dictionary<long, float> fScore = new Dictionary<long, float>();

        var watch = new System.Diagnostics.Stopwatch();
        int nodesVisited = 0;

        gScore[start] = 0;
        fScore[start] = 0;
        parent[start] = -1;
        openList.Enqueue(start, fScore[start]);

        while (openList.Count > 0)
        {
            long current = openList.Dequeue();
            

            if (current == end)
            {
                MapController.DisplayStatistics(start, end, gScore[current], stopwatch.ElapsedMilliseconds, nodesVisited);
                UnityEngine.Debug.Log("Nodes visited: " + nodesVisited);
                return (gScore[current], ReconstructPath(parent, start, end));
            }
            nodesVisited++;


            watch.Start();
            Edge[] neighbors = graph.GetNeighbors(current);
            foreach (Edge neighbor in neighbors)
            {
                if (current == end)
                {
                    parent[neighbor.node] = current;
                    Debug.Log("Nodes visited: " + nodesVisited);
                    return (gScore[current], ReconstructPath(parent, start, end));
                }

                float tentativeGScore = gScore[current] + neighbor.cost;
                float heuristicCostEstimate = HeuristicCostEstimate(neighbor.node, end);
                nodesVisited++;
                float neighborFScore = tentativeGScore + heuristicCostEstimate;

                //gScore[neighbor.node] = tentativeGScore;
                //fScore[neighbor.node] = neighborFScore;

                if (openList.Contains(neighbor.node) && fScore[neighbor.node] < neighborFScore)
                {
                    continue;
                }
                else if (closedSet.Contains(neighbor.node) && fScore[neighbor.node] < neighborFScore)
                {
                    continue;
                }
                parent[neighbor.node] = current;
                gScore[neighbor.node] = tentativeGScore;
                fScore[neighbor.node] = neighborFScore;
                Console.WriteLine("Adding to open list: " + neighbor.node);
                if (openList.Contains(neighbor.node))
                {
                    openList.UpdatePriority(neighbor.node, fScore[neighbor.node]);
                }
                else
                {
                    openList.Enqueue(neighbor.node, fScore[neighbor.node]);
                }
            }
            closedSet.Add(current);
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
        Debug.Log("Reconstructing path " + cameFrom.Count);
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