using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class Benchmarks
{
    Graph dach_farthest;
    Graph dach_random;
    (long, long)[] stPairs;
    int ROUTE_AMOUNT = 500    ; //amount of routes to benchmark

    [OneTimeSetUp]
    public void TestInitialize()
    {
        var random = new System.Random();

        dach_farthest = MapController.DeserializeGraph("Assets/Maps/dach.graph"); 
        dach_random = MapController.DeserializeGraph("Assets/Maps/dach_random_landmarks.graph");

        var stPairs = new (long, long)[ROUTE_AMOUNT];
        for (int i = 0; i < ROUTE_AMOUNT; i++)
        {
            var startNode = GetRandomNode(random, dach_farthest);
            var endNode = GetRandomNode(random, dach_farthest);
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
    public List<PathResult> BenchmarkAlgorithm((long, long)[] stPairs, IPathfindingAlgorithm algorithm, string fileout, float epsilon = 1, List<PathResult> expectedResults = null)
    {
        var filePath = Application.dataPath + "/../BenchmarkData/" + fileout + ".csv";
        var results = new List<PathResult>();
        foreach (var pair in stPairs)
        {
            var startNode = pair.Item1;
            var endNode = pair.Item2;
            var expectedDistance = 0f;
            if (expectedResults != null)
            {
                expectedDistance = expectedResults.FirstOrDefault(x => x.start == startNode && x.end == endNode)?.distance ?? 0;
                if (expectedDistance != 0)
                {
                    var pathResult = algorithm.FindShortestPath(startNode, endNode);
                    if (pathResult == null) continue;   // If no path is found, skip the result
                    results.Add(pathResult);
                    Assert.AreEqual(expectedDistance, pathResult.distance, epsilon, message: "Distance mismatch for " + startNode + " -> " + endNode);
                }
            }
            else {
                var pathResult = algorithm.FindShortestPath(startNode, endNode);
                if (pathResult == null) continue;   // If no path is found, skip the result
                results.Add(pathResult);
            }

        }
        var csv = new System.Text.StringBuilder();
        foreach (var result in results)
        {
            csv.AppendLine(result.start + ";" + result.end + ";" + result.distance.ToString().Replace('.',',') + ";" + result.miliseconds + ";" + result.nodesVisited);
        }
        // If file already exists, append the lines to the file
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.AppendAllText(filePath, csv.ToString());
            return results;
        }
        else {
            csv.Insert(0, "StartNode;EndNode;Distance;Time;Nodes visited\n");  
            System.IO.File.WriteAllText(filePath, csv.ToString());
        }
        return results;
    }

    [Test]
    public void BenchmarkAllAlgorithms()
    {
        var dijkstra = new Dijkstra(dach_farthest);
        var dijkstraResults = BenchmarkAlgorithm(stPairs, dijkstra, "dijkstra");

        var biDijkstra = new BiDijkstra(dach_farthest);
        var biAstar = new BiAStar(dach_farthest);
        var aStar = new AStar(dach_farthest);
        var landmarks = new Landmarks(dach_farthest, showLandmarks: false);
        var landmarks_300 = new Landmarks(dach_farthest, showLandmarks: false, updateLandmarks: 300);

        var landmarks_random = new Landmarks(dach_random, showLandmarks: false);
        var landmarks_300_random = new Landmarks(dach_random, showLandmarks: false, updateLandmarks: 300);
        BenchmarkAlgorithm(stPairs, biDijkstra, "biDijkstra", expectedResults: dijkstraResults);
        BenchmarkAlgorithm(stPairs, biAstar, "biAstar", expectedResults: dijkstraResults);
        BenchmarkAlgorithm(stPairs, aStar, "aStar", expectedResults: dijkstraResults);
        BenchmarkAlgorithm(stPairs, landmarks, "landmarks", expectedResults: dijkstraResults);
        BenchmarkAlgorithm(stPairs, landmarks_300, "landmarks_300", expectedResults: dijkstraResults);
        BenchmarkAlgorithm(stPairs, landmarks_random, "landmarks_random", epsilon:1000,expectedResults: dijkstraResults);
        BenchmarkAlgorithm(stPairs, landmarks_300_random, "landmarks_300_random", epsilon:1000, expectedResults: dijkstraResults);

    }
}