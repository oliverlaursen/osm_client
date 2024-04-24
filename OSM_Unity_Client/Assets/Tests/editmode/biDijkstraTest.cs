using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class NewTestScript
{
    Graph denmarkGraph;
    // A Test behaves as an ordinary method
    [Test]
    public void FindsCorrectDistance()
    {
        var denmarkGraph = MapController.DeserializeGraph("Assets/Maps/denmark.graph");
        long startNode = 280276691;
        long endNode = 896780523;
        double expectedDistance = 324741;

        BiDijkstra biDijkstra = new BiDijkstra(denmarkGraph);
        var pathResult = biDijkstra.FindShortestPath(startNode, endNode);
        var distance = pathResult.distance;
        Assert.AreEqual(expectedDistance, distance);
    }
}
