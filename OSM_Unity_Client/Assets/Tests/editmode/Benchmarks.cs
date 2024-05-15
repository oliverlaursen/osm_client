using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.TestTools;

public class Benchmarks
{
    Graph denmarkGraph;
    (long, long)[] stPairs;
    int ROUTE_AMOUNT = 200; //amount of routes to benchmark

    [OneTimeSetUp]
    public void TestInitialize()
    {
        var random = new System.Random();

         var denmarkGraph = MapController.DeserializeGraph("Assets/Maps/denmark.graph"); 

        var stPairs = new (long, long)[ROUTE_AMOUNT];
        for (int i = 0; i < ROUTE_AMOUNT; i++)
        {
            var startNode = GetRandomNode(random, denmarkGraph);
            var endNode = GetRandomNode(random, denmarkGraph);
            stPairs[i] = (startNode, endNode);
        }
        this.stPairs = stPairs;
    }

    public static long GetRandomNode(System.Random random, Graph graph)
    {
        return random.Next(graph.nodes.Length-1);
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
    public List<PathResult> BenchmarkAlgorithm((long, long)[] stPairs, IPathfindingAlgorithm algorithm, string fileout, List<PathResult> expectedResults = null)
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
            if (expectedResults != null)
            {
                var expectedDistance = expectedResults.First(x => x.start == startNode && x.end == endNode).distance;
                Assert.AreEqual(expectedDistance, pathResult.distance, message: "Distance mismatch for " + startNode + " -> " + endNode);
            }   
        }
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("StartNode;EndNode;Distance;Time;Nodes visited");
        foreach (var result in results)
        {
            csv.AppendLine(result.start + ";" + result.end + ";" + result.distance + ";" + result.miliseconds + ";" + result.nodesVisited);
        }
        System.IO.File.WriteAllText(filePath, csv.ToString());
        return results;
    }

    [Test]
    public void BenchmarkAllAlgorithms()
    {
        var dijkstra = new Dijkstra(denmarkGraph);
        var dijkstraResults = BenchmarkAlgorithm(stPairs, dijkstra, "dijkstra");

        var biDijkstra = new BiDijkstra(denmarkGraph);
        var biAstar = new BiAStar(denmarkGraph);
        var aStar = new AStar(denmarkGraph);
        var landmarks = new Landmarks(denmarkGraph, showLandmarks: false);
        var landmarks_300 = new Landmarks(denmarkGraph, showLandmarks: false, updateLandmarks: 300);
        BenchmarkAlgorithm(stPairs, biDijkstra, "biDijkstra", expectedResults: dijkstraResults);
        BenchmarkAlgorithm(stPairs, biAstar, "biAstar", expectedResults: dijkstraResults);
        BenchmarkAlgorithm(stPairs, aStar, "aStar", expectedResults: dijkstraResults);
        BenchmarkAlgorithm(stPairs, landmarks, "landmarks", expectedResults: dijkstraResults);
        BenchmarkAlgorithm(stPairs, landmarks_300, "landmarks_300", expectedResults: dijkstraResults);

    }
}