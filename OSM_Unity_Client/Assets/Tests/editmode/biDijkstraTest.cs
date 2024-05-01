using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BiDijkstraTests
{
    int COMPARISON_AMOUNT = 10; //amount of times to compare bidijkstra distance to dijkstra
    Graph denmarkGraph;
    Dijkstra dijkstra;
    BiDijkstra biDijkstra;

    public void initializeTest()
    {
        denmarkGraph = MapController.DeserializeGraph("Assets/Maps/denmark.graph");
        dijkstra = new Dijkstra(denmarkGraph);
        biDijkstra = new BiDijkstra(denmarkGraph);
    }

    public KeyValuePair<long, (float[], double[])> GetRandomNode()
    {
        var random = new System.Random();
        var index = random.Next(denmarkGraph.nodes.Count);
        var node = new List<KeyValuePair<long, (float[], double[])>>(denmarkGraph.nodes)[index];
        return node;
    }

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
    
    [Test]
    public void test()
    {
        initializeTest();

        for (int i = 0; i < COMPARISON_AMOUNT; i++)
        {
            var node = GetRandomNode();
            var node2 = GetRandomNode();

            var startNode = node.Key;
            var endNode = node2.Key;

            var dijkstraPathResult = dijkstra.FindShortestPath(startNode, endNode);
            var biDijkstraPathResult = biDijkstra.FindShortestPath(startNode, endNode);

            Assert.AreEqual(dijkstraPathResult.distance, biDijkstraPathResult.distance);
            Debug.Log(i);
        }
    }

}
