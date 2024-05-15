using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


public class AstarTests
{
    int COMPARISON_AMOUNT = 10; //amount of times to compare astar distance to dijkstra
    Graph denmarkGraph;
    Graph dachGraph;
    Dijkstra dijkstra;
    AStar astar;

    [OneTimeSetUp]
    public void InitializeTest()
    {
        denmarkGraph = MapController.DeserializeGraph("Assets/Maps/denmark.graph");
        astar = new AStar(denmarkGraph);
        dijkstra = new Dijkstra(denmarkGraph);
    }


    // generates x amount of random nodes and checks that astar and dijkstra calculates the same distace
    [Test]
    public void AstarHasSameDistanceAndPathAsDijkstra()
    {
        var random = new System.Random();
        var node = Benchmarks.GetRandomNode(random, denmarkGraph);
        var node2 = Benchmarks.GetRandomNode(random, denmarkGraph);

        long startNode = node;
        long endNode = node2;

        for (int i = 0; i < COMPARISON_AMOUNT; i++)
        {
            var dijkstraPathResult = dijkstra.FindShortestPath(startNode, endNode);
            if (dijkstraPathResult == null) continue;   // If no path is found, skip the test
            var astarPathResult = astar.FindShortestPath(startNode, endNode);

            Assert.AreEqual(dijkstraPathResult.distance, astarPathResult.distance);
            Assert.AreEqual(dijkstraPathResult.path, astarPathResult.path);
        }
    }
}
