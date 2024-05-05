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


    [Test]
    /*
    Edges for 312310192:
    Node: 312310202 Cost: 613
    Node: 312310296 Cost: 691
    Node: 312310777 Cost: 2524
    Node: 1313060778 Cost: 1582
    Bi-Edges for 312310192: 
    Node: 312310202 Cost: 613
    Node: 312310296 Cost: 691
    Node: 1313060778 Cost: 1582
    Node: 312310777 Cost: 2524

    Edges for 312310202:
    Node: 311660177 Cost: 3501
    Node: 312310192 Cost: 613
    Node: 312310296 Cost: 574
    Node: 2715370639 Cost: 7446
    Bi-Edges for 312310202: 
    Node: 312310192 Cost: 613
    Node: 312310296 Cost: 574
    Node: 311660177 Cost: 3501
    Node: 2715370639 Cost: 7446

    Edges for 311660177:
    Node: 312310202 Cost: 3501
    Node: 564295783 Cost: 45
    Bi-Edges for 311660177: 
    Node: 312310202 Cost: 3501
    Node: 311660134 Cost: 75
    Node: 564295783 Cost: 45

    Edges for 311660134:
    Node: 311660177 Cost: 75
    Node: 564295783 Cost: 53
    Node: 1239728955 Cost: 1485
    Bi-Edges for 311660134: 
    Node: 1239728955 Cost: 1485
    Node: 564295783 Cost: 53

    Edges for 564295783:
    Node: 311660134 Cost: 53
    Node: 311660177 Cost: 45
    Node: 312311692 Cost: 820
    Bi-Edges for 564295783: 
    Node: 311660134 Cost: 53
    Node: 311660177 Cost: 45
    Node: 312311692 Cost: 820

    Edges for 312311692:
    Node: 312311600 Cost: 47
    Node: 564295783 Cost: 820
    Node: 2715370503 Cost: 60
    Bi-Edges for 312311692: 
    Node: 2715370503 Cost: 60
    Node: 564295783 Cost: 820
    Node: 312311600 Cost: 47

    Edges for 2715370503:
    Node: 312310777 Cost: 874
    Node: 312311600 Cost: 83
    Node: 312311692 Cost: 60
    Bi-Edges for 2715370503: 
    Node: 312311692 Cost: 60
    Node: 312310777 Cost: 874

    Edges for 312310777:
    Node: 312310192 Cost: 2524
    Node: 312310920 Cost: 159
    Node: 312311676 Cost: 612
    Node: 2715370503 Cost: 874
    Bi-Edges for 312310777: 
    Node: 312310192 Cost: 2524
    Node: 2715370503 Cost: 874
    Node: 312310920 Cost: 159
    Node: 312311676 Cost: 612








    */
    public void isolatedTest(){
        Dictionary<long, Edge[]> graph = new();
        graph[312310192] = new Edge[]{new Edge{node = 312310202, cost = 613}, new Edge{node = 312310296, cost = 691}, new Edge{node = 312310777, cost = 2524}, new Edge{node = 1313060778, cost = 1582}};
        graph[312310202] = new Edge[]{new Edge{node = 311660177, cost = 3501}, new Edge{node = 312310192, cost = 613}, new Edge{node = 312310296, cost = 574}, new Edge{node = 2715370639, cost = 7446}};
        graph[311660177] = new Edge[]{new Edge{node = 312310202, cost = 3501}, new Edge{node = 564295783, cost = 45}};
        graph[311660134] = new Edge[]{new Edge{node = 311660177, cost = 75}, new Edge{node = 564295783, cost = 53}, new Edge{node = 1239728955, cost = 1485}};
        graph[564295783] = new Edge[]{new Edge{node = 311660134, cost = 53}, new Edge{node = 311660177, cost = 45}, new Edge{node = 312311692, cost = 820}};
        graph[312311692] = new Edge[]{new Edge{node = 312311600, cost = 47}, new Edge{node = 564295783, cost = 820}, new Edge{node = 2715370503, cost = 60}};
        graph[2715370503] = new Edge[]{new Edge{node = 312310777, cost = 874}, new Edge{node = 312311600, cost = 83}, new Edge{node = 312311692, cost = 60}};
        graph[312310777] = new Edge[]{new Edge{node = 312310192, cost = 2524}, new Edge{node = 312310920, cost = 159}, new Edge{node = 312311676, cost = 612}, new Edge{node = 2715370503, cost = 874}};
        graph[312310296] = new Edge[]{};
        graph[312310920] = new Edge[]{};
        graph[312311676] = new Edge[]{};
        graph[312311600] = new Edge[]{};
        graph[1313060778] = new Edge[]{};
        graph[2715370639] = new Edge[]{};
        graph[1239728955] = new Edge[]{};


        Dictionary<long, Edge[]> bi_graph = new();
        bi_graph[312310192] = new Edge[]{new Edge{node = 312310202, cost = 613}, new Edge{node = 312310296, cost = 691}, new Edge{node = 1313060778, cost = 1582}, new Edge{node = 312310777, cost = 2524}};
        bi_graph[312310202] = new Edge[]{new Edge{node = 312310192, cost = 613}, new Edge{node = 312310296, cost = 574}, new Edge{node = 311660177, cost = 3501}, new Edge{node = 2715370639, cost = 7446}};
        bi_graph[311660177] = new Edge[]{new Edge{node = 312310202, cost = 3501}, new Edge{node = 311660134, cost = 75}, new Edge{node = 564295783, cost = 45}};
        bi_graph[311660134] = new Edge[]{new Edge{node = 1239728955, cost = 1485}, new Edge{node = 564295783, cost = 53}};
        bi_graph[564295783] = new Edge[]{new Edge{node = 311660134, cost = 53}, new Edge{node = 311660177, cost = 45}, new Edge{node = 312311692, cost = 820}};
        bi_graph[312311692] = new Edge[]{new Edge{node = 312311600, cost = 47}, new Edge{node = 564295783, cost = 820}, new Edge{node = 2715370503, cost = 60}};
        bi_graph[2715370503] = new Edge[]{new Edge{node = 312311692, cost = 60}, new Edge{node = 312310777, cost = 874}};
        bi_graph[312310777] = new Edge[]{new Edge{node = 312310192, cost = 2524}, new Edge{node = 2715370503, cost = 874}, new Edge{node = 312310920, cost = 159}, new Edge{node = 312311676, cost = 612}};
        bi_graph[312310296] = new Edge[]{};
        bi_graph[312310920] = new Edge[]{};
        bi_graph[312311676] = new Edge[]{};
        bi_graph[312311600] = new Edge[]{};
        bi_graph[1313060778] = new Edge[]{};
        bi_graph[2715370639] = new Edge[]{};
        bi_graph[1239728955] = new Edge[]{};

        Dictionary<long, (float[], double[])> nodes = new();
        nodes[312310192] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[312310202] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[311660177] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[311660134] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[564295783] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[312311692] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[2715370503] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[312310777] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[312310296] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[312310920] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[312311676] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[312311600] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[1313060778] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[2715370639] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[1239728955] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        List<Landmark> landmarks = new();

        Graph smallGraph = new Graph{nodes = nodes, graph = graph, bi_graph = bi_graph, landmarks = landmarks};
        BiDijkstra biDijkstra = new BiDijkstra(smallGraph);
        Dijkstra dijkstra = new Dijkstra(smallGraph);
        var dijkstraResult = dijkstra.FindShortestPath(312310192, 311660134);
        var bidijkstraResult = biDijkstra.FindShortestPath(312310192, 311660134);
        Assert.AreEqual(dijkstra.distances, bidijkstraResult.distance);
    }

}
