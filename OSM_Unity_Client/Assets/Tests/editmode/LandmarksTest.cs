using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class LandmarksTest
{
    int COMPARISON_AMOUNT = 10; //amount of times to compare astar distance to dijkstra
    Graph denmarkGraph;
    Dijkstra dijkstra;
    Landmarks landmarks;

    [SetUp]
    public void InitializeTest()
    {
        denmarkGraph = MapController.DeserializeGraph("Assets/Maps/dach.graph");
        landmarks = new Landmarks(denmarkGraph);
        dijkstra = new Dijkstra(denmarkGraph);
    }

    // generates x amount of random nodes and checks that astar and dijkstra calculates the same distace
    [Test]
    public void LandmarksHasSameDistanceAndPathAsDijkstra()
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
            var astarPathResult = landmarks.FindShortestPath(startNode, endNode);

            Assert.AreEqual(dijkstraPathResult.distance, astarPathResult.distance);
            Assert.AreEqual(dijkstraPathResult.path, astarPathResult.path);
        }
    }
}
