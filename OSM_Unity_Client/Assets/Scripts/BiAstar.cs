using System;
using System.Collections;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;
using UnityEngine.Assertions;


public class BiAStar : IPathfindingAlgorithm
{
    public Graph graph;
    public BiAStar(Graph graph)
    {
        this.graph = graph;
    }

    public void FindShortestPath(long start, long end)
    {
        var astar1 = new AStar(graph);
        var astar2 = new AStar(graph);
        astar1.InitializeSearch(start, end);
        astar2.InitializeSearch(end, start);
        int nodesVisited = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var openList = new FastPriorityQueue<AStar.PriorityQueueNode>(graph.nodes.Count);
        var openSet = new HashSet<long>();


        while(openList.Count > 0)
        {
            long current = AStar.DequeueAndUpdateSets(openList, openSet);
            if (astar1.ProcessCurrentNode(current, start, end, ref nodesVisited, stopwatch)) return;
            
        }

        throw new NotImplementedException();
    }

    public IEnumerator FindShortestPathWithVisual(long start, long end, int drawspeed)
    {
        throw new NotImplementedException();
    }
}