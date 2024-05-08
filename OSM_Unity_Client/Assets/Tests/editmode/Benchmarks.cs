using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.TestTools;

public class Benchmarks
{
    Graph denmarkGraph;
    (long, long)[] stPairs;
    int ROUTE_AMOUNT = 10; //amount of routes to benchmark

    [SetUp]
    public void TestInitialize()
    {
        var random = new System.Random();
        denmarkGraph = MapController.DeserializeGraph("Assets/Maps/dach.graph");

        var stPairs = new (long, long)[ROUTE_AMOUNT];
        for (int i = 0; i < ROUTE_AMOUNT; i++)
        {
            var startNode = GetRandomNode(random, denmarkGraph).Key;
            var endNode = GetRandomNode(random, denmarkGraph).Key;
            stPairs[i] = (startNode, endNode);
        }
        this.stPairs = stPairs;
    }

    public KeyValuePair<long, (float[], double[])> GetRandomNode(System.Random random, Graph graph)
    {
        var index = random.Next(graph.nodes.Count);
        var node = new List<KeyValuePair<long, (float[], double[])>>(graph.nodes)[index];
        return node;
    }

    /*
    * BenchmarkAlgorithm is a method that benchmarks the performance of a pathfinding algorithm.
    * The method takes in an array of start and target pairs, the pathfinding algorithm to be benchmarked,
    * and the name of the file to write the results to
    * The method writes the results of the benchmark to the file specified (in CSV format).
    *
    * The method also takes an optional parameter expectedDistances, which is an array of expected distances
    * for each start-target pair. If this parameter is provided, the method will assert that the distance
    * returned by the algorithm matches the expected distance.
    * 
    * The method returns a list of PathResults, which contain the results of the benchmark for each start-target pair.
    */
    public List<PathResult> BenchmarkAlgorithm((long, long)[] stPairs, IPathfindingAlgorithm algorithm, string fileout, float[] expectedDistances = null)
    {
        var filePath = Application.dataPath + "/../BenchmarkData/" + fileout + ".csv";
        var results = new List<PathResult>();
        foreach (var pair in stPairs)
        {
            var startNode = pair.Item1;
            var endNode = pair.Item2;
            var pathResult = algorithm.FindShortestPath(startNode, endNode);
            if (pathResult == null) continue;   // If no path is found, skip the result
            results.Add(pathResult);
            if (expectedDistances != null)
            {
                var expectedDistance = expectedDistances[Array.IndexOf(stPairs, pair)];
                Assert.AreEqual(expectedDistance, pathResult.distance, message: "Distance mismatch for " + startNode + " -> " + endNode);
            }   
        }
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("StartNode,EndNode,Distance,Time,Nodes visited");
        foreach (var result in results)
        {
            csv.AppendLine(result.start + "," + result.end + "," + result.distance + "," + result.miliseconds + "," + result.nodesVisited);
        }
        System.IO.File.WriteAllText(filePath, csv.ToString());
        return results;
    }

    [Test]
    public void BenchmarkAllAlgorithms()
    {
        var dijkstra = new Dijkstra(denmarkGraph);
        var dijkstraResults = BenchmarkAlgorithm(stPairs, dijkstra, "dijkstra");
        var dijkstraDistances = dijkstraResults.Select(x => x.distance).ToArray();

        var biDijkstra = new BiDijkstra(denmarkGraph);
        var biAstar = new BiAStar(denmarkGraph);
        var aStar = new AStar(denmarkGraph);
        var landmarks = new Landmarks(denmarkGraph, showLandmarks: false);
        BenchmarkAlgorithm(stPairs, biDijkstra, "biDijkstra", expectedDistances: dijkstraDistances);
        BenchmarkAlgorithm(stPairs, biAstar, "biAstar", expectedDistances: dijkstraDistances);
        BenchmarkAlgorithm(stPairs, aStar, "aStar", expectedDistances: dijkstraDistances);
        BenchmarkAlgorithm(stPairs, landmarks, "landmarks", expectedDistances: dijkstraDistances);
    }
}