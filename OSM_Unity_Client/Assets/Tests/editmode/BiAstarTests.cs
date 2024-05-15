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

    [SetUp]
    public void InitializeTest()
    {
        denmarkGraph = MapController.DeserializeGraph("Assets/Maps/denmark.graph");
        biAstar = new BiAStar(denmarkGraph);
        dijkstra = new Dijkstra(denmarkGraph);
    }

    // generates x amount of random nodes and checks that astar and dijkstra calculates the same distace
    [Test]
    public void BiAstarHasSameDistanceAndPathAsDijkstra()
    {
        var random = new System.Random();
        for (int i = 0; i < COMPARISON_AMOUNT; i++)
        {
            var node = Benchmarks.GetRandomNode(random, denmarkGraph);
            var node2 = Benchmarks.GetRandomNode(random, denmarkGraph);

            long startNode = node;
            long endNode = node2;
            var dijkstraPathResult = dijkstra.FindShortestPath(startNode, endNode);
            if (dijkstraPathResult == null) continue;   // If no path is found, skip the test
            var astarPathResult = biAstar.FindShortestPath(startNode, endNode);

            Assert.AreEqual(dijkstraPathResult.distance, astarPathResult.distance);
            Assert.AreEqual(dijkstraPathResult.path, astarPathResult.path);
        }
    }
}
