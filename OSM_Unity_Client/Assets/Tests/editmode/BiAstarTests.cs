using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


public class BiAstarTests
{
    int COMPARISON_AMOUNT = 10; //amount of times to compare astar distance to dijkstra
    Graph denmarkGraph;
    Dijkstra dijkstra;
    BiAStar biAstar;

    public void InitializeTest()
    {
        denmarkGraph = MapController.DeserializeGraph("Assets/Maps/denmark.graph");
        biAstar = new BiAStar(denmarkGraph);
        dijkstra = new Dijkstra(denmarkGraph);
    }

    public KeyValuePair<long, (float[], double[])> GetRandomNode()
    {
        var random = new System.Random();
        var index = random.Next(denmarkGraph.nodes.Count);
        var node = new List<KeyValuePair<long, (float[], double[])>>(denmarkGraph.nodes)[index];
        return node;
    }

    // generates x amount of random nodes and checks that astar and dijkstra calculates the same distace
    [Test]
    public void BiAstarHasSameDistanceAndPathAsDijkstra()
    {
        InitializeTest();
        for (int i = 0; i < COMPARISON_AMOUNT; i++)
        {
            var node = GetRandomNode();
            var node2 = GetRandomNode();

            long startNode = node.Key;
            long endNode = node2.Key;
            var dijkstraPathResult = dijkstra.FindShortestPath(startNode, endNode);
            if (dijkstraPathResult == null) continue;   // If no path is found, skip the test
            var astarPathResult = biAstar.FindShortestPath(startNode, endNode);

            Assert.AreEqual(dijkstraPathResult.distance, astarPathResult.distance);
            Assert.AreEqual(dijkstraPathResult.path, astarPathResult.path);
        }
    }
}
