using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathResult {
    public long start;
    public long end;
    public float distance;
    public long miliseconds;
    public int nodesVisited;
    public long[] path;

    public PathResult(long start, long end, float distance, long miliseconds, int nodesVisited, long[] path)
    {
        this.start = start;
        this.end = end;
        this.distance = distance;
        this.miliseconds = miliseconds;
        this.nodesVisited = nodesVisited;
        this.path = path;
    }
    public void DisplayAndDrawPath(Graph graph){
        MapController.DisplayStatistics(start, end, distance, miliseconds, nodesVisited);
        GameObject.Find("Map").GetComponent<MapController>().DrawPath(graph.nodes, path);
    }
}

public interface IPathfindingAlgorithm
{
    public PathResult FindShortestPath(long start, long end);

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed);
}
