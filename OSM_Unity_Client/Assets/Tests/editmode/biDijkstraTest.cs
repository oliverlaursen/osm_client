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
    Edges for 1:
    Node: 2 Cost: 613
    Node: 9 Cost: 691
    Node: 8 Cost: 2524
    Node: 13 Cost: 1582
    Bi-Edges for 1: 
    Node: 2 Cost: 613
    Node: 9 Cost: 691
    Node: 13 Cost: 1582
    Node: 8 Cost: 2524

    Edges for 2:
    Node: 3 Cost: 3501
    Node: 1 Cost: 613
    Node: 9 Cost: 574
    Node: 14 Cost: 7446
    Bi-Edges for 2: 
    Node: 1 Cost: 613
    Node: 9 Cost: 574
    Node: 3 Cost: 3501
    Node: 14 Cost: 7446

    Edges for 3:
    Node: 2 Cost: 3501
    Node: 5 Cost: 45
    Bi-Edges for 3: 
    Node: 2 Cost: 3501
    Node: 4 Cost: 75
    Node: 5 Cost: 45

    Edges for 4:
    Node: 3 Cost: 75
    Node: 5 Cost: 53
    Node: 15 Cost: 1485
    Bi-Edges for 4: 
    Node: 15 Cost: 1485
    Node: 5 Cost: 53

    Edges for 5:
    Node: 4 Cost: 53
    Node: 3 Cost: 45
    Node: 6 Cost: 820
    Bi-Edges for 5: 
    Node: 4 Cost: 53
    Node: 3 Cost: 45
    Node: 6 Cost: 820

    Edges for 6:
    Node: 12 Cost: 47
    Node: 5 Cost: 820
    Node: 7 Cost: 60
    Bi-Edges for 6: 
    Node: 7 Cost: 60
    Node: 5 Cost: 820
    Node: 12 Cost: 47

    Edges for 7:
    Node: 8 Cost: 874
    Node: 12 Cost: 83
    Node: 6 Cost: 60
    Bi-Edges for 7: 
    Node: 6 Cost: 60
    Node: 8 Cost: 874

    Edges for 8:
    Node: 1 Cost: 2524
    Node: 10 Cost: 159
    Node: 11 Cost: 612
    Node: 7 Cost: 874
    Bi-Edges for 8: 
    Node: 1 Cost: 2524
    Node: 7 Cost: 874
    Node: 10 Cost: 159
    Node: 11 Cost: 612








    */
    public void isolatedTest(){
        Dictionary<long, Edge[]> graph = new();
        graph[1] = new Edge[]{new Edge{node = 2, cost = 613}, new Edge{node = 9, cost = 691}, new Edge{node = 8, cost = 2524}, new Edge{node = 13, cost = 1582}};
        graph[2] = new Edge[]{new Edge{node = 3, cost = 3501}, new Edge{node = 1, cost = 613}, new Edge{node = 9, cost = 574}, new Edge{node = 14, cost = 7446}};
        graph[3] = new Edge[]{new Edge{node = 2, cost = 3501}, new Edge{node = 5, cost = 45}};
        graph[4] = new Edge[]{new Edge{node = 3, cost = 75}, new Edge{node = 5, cost = 53}, new Edge{node = 15, cost = 1485}};
        graph[5] = new Edge[]{new Edge{node = 4, cost = 53}, new Edge{node = 3, cost = 45}, new Edge{node = 6, cost = 820}};
        graph[6] = new Edge[]{new Edge{node = 12, cost = 47}, new Edge{node = 5, cost = 820}, new Edge{node = 7, cost = 60}};
        graph[7] = new Edge[]{new Edge{node = 8, cost = 874}, new Edge{node = 12, cost = 83}, new Edge{node = 6, cost = 60}};
        graph[8] = new Edge[]{new Edge{node = 1, cost = 2524}, new Edge{node = 10, cost = 159}, new Edge{node = 11, cost = 612}, new Edge{node = 7, cost = 874}};
        graph[9] = new Edge[]{};
        graph[10] = new Edge[]{};
        graph[11] = new Edge[]{};
        graph[12] = new Edge[]{};
        graph[13] = new Edge[]{};
        graph[14] = new Edge[]{};
        graph[15] = new Edge[]{};


        Dictionary<long, Edge[]> bi_graph = new();
        bi_graph[1] = new Edge[]{new Edge{node = 2, cost = 613}, new Edge{node = 9, cost = 691}, new Edge{node = 13, cost = 1582}, new Edge{node = 8, cost = 2524}};
        bi_graph[2] = new Edge[]{new Edge{node = 1, cost = 613}, new Edge{node = 9, cost = 574}, new Edge{node = 3, cost = 3501}, new Edge{node = 14, cost = 7446}};
        bi_graph[3] = new Edge[]{new Edge{node = 2, cost = 3501}, new Edge{node = 4, cost = 75}, new Edge{node = 5, cost = 45}};
        bi_graph[4] = new Edge[]{new Edge{node = 15, cost = 1485}, new Edge{node = 5, cost = 53}};
        bi_graph[5] = new Edge[]{new Edge{node = 4, cost = 53}, new Edge{node = 3, cost = 45}, new Edge{node = 6, cost = 820}};
        bi_graph[6] = new Edge[]{new Edge{node = 12, cost = 47}, new Edge{node = 5, cost = 820}, new Edge{node = 7, cost = 60}};
        bi_graph[7] = new Edge[]{new Edge{node = 6, cost = 60}, new Edge{node = 8, cost = 874}};
        bi_graph[8] = new Edge[]{new Edge{node = 1, cost = 2524}, new Edge{node = 7, cost = 874}, new Edge{node = 10, cost = 159}, new Edge{node = 11, cost = 612}};
        bi_graph[9] = new Edge[]{};
        bi_graph[10] = new Edge[]{};
        bi_graph[11] = new Edge[]{};
        bi_graph[12] = new Edge[]{};
        bi_graph[13] = new Edge[]{};
        bi_graph[14] = new Edge[]{};
        bi_graph[15] = new Edge[]{};

        Dictionary<long, (float[], double[])> nodes = new();
        nodes[1] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[2] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[3] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[4] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[5] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[6] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[7] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[8] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[9] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[10] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[11] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[12] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[13] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[14] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[15] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        List<Landmark> landmarks = new();

        Graph smallGraph = new Graph{nodes = nodes, graph = graph, bi_graph = bi_graph, landmarks = landmarks};
        BiDijkstra biDijkstra = new BiDijkstra(smallGraph);
        Dijkstra dijkstra = new Dijkstra(smallGraph);
        var dijkstraResult = dijkstra.FindShortestPath(1, 4);
        var bidijkstraResult = biDijkstra.FindShortestPath(1, 4);
        Assert.AreEqual(dijkstraResult.distance, bidijkstraResult.distance);
    }

}
