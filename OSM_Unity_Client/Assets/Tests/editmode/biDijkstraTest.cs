using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BiDijkstraTests
{
    int COMPARISON_AMOUNT = 10; //amount of times to compare bidijkstra distance to dijkstra
    Graph denmarkGraph;
    Dijkstra dijkstra;
    BiDijkstra biDijkstra;



    [OneTimeSetUp]
    public void InitializeTest()
    {
        denmarkGraph = MapController.DeserializeGraph("Assets/Maps/denmark.graph");
        dijkstra = new Dijkstra(denmarkGraph);
        biDijkstra = new BiDijkstra(denmarkGraph);
    }
    
    [Test]
    public void TenRandomRoutesBiDijkstra()
    {
        var random = new System.Random();
        for (int i = 0; i < COMPARISON_AMOUNT; i++)
        {
            var node = Benchmarks.GetRandomNode(random, denmarkGraph);
            var node2 = Benchmarks.GetRandomNode(random, denmarkGraph);

            var startNode = node;
            var endNode = node2;

            var dijkstraPathResult = dijkstra.FindShortestPath(startNode, endNode);
            if (dijkstraPathResult == null) continue;   // If no path is found, skip the test
            var biDijkstraPathResult = biDijkstra.FindShortestPath(startNode, endNode);

            Assert.AreEqual(dijkstraPathResult.distance, biDijkstraPathResult.distance);
            Debug.Log(i);
        }
    }


    [Test]
    public void IsolatedTest(){
        Edge[][] graph = new Edge[9][];
        graph[1] = new Edge[]{new Edge{node = 2, cost = 613}, new Edge{node = 8, cost = 2524}};
        graph[2] = new Edge[]{new Edge{node = 3, cost = 3501}, new Edge{node = 1, cost = 613}};
        graph[3] = new Edge[]{new Edge{node = 2, cost = 3501}, new Edge{node = 5, cost = 45}};
        graph[4] = new Edge[]{new Edge{node = 3, cost = 75}, new Edge{node = 5, cost = 53}};
        graph[5] = new Edge[]{new Edge{node = 4, cost = 53}, new Edge{node = 3, cost = 45}, new Edge{node = 6, cost = 820}};
        graph[6] = new Edge[]{new Edge{node = 5, cost = 820}, new Edge{node = 7, cost = 60}};
        graph[7] = new Edge[]{new Edge{node = 8, cost = 874}, new Edge{node = 6, cost = 60}};
        graph[8] = new Edge[]{new Edge{node = 1, cost = 2524}, new Edge{node = 7, cost = 874}};



        Edge[][] bi_graph = new Edge[9][];
        bi_graph[1] = new Edge[]{new Edge{node = 2, cost = 613}, new Edge{node = 8, cost = 2524}};
        bi_graph[2] = new Edge[]{new Edge{node = 1, cost = 613}, new Edge{node = 3, cost = 3501}};
        bi_graph[3] = new Edge[]{new Edge{node = 2, cost = 3501}, new Edge{node = 4, cost = 75}, new Edge{node = 5, cost = 45}};
        bi_graph[4] = new Edge[]{new Edge{node = 5, cost = 53}};
        bi_graph[5] = new Edge[]{new Edge{node = 4, cost = 53}, new Edge{node = 3, cost = 45}, new Edge{node = 6, cost = 820}};
        bi_graph[6] = new Edge[]{new Edge{node = 5, cost = 820}, new Edge{node = 7, cost = 60}};
        bi_graph[7] = new Edge[]{new Edge{node = 6, cost = 60}, new Edge{node = 8, cost = 874}};
        bi_graph[8] = new Edge[]{new Edge{node = 1, cost = 2524}, new Edge{node = 7, cost = 874}};


        (float[], double[])[] nodes = new (float[], double[])[9];
        nodes[1] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[2] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[3] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[4] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[5] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[6] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[7] = (new float[]{12.0f, 55.0f}, new double[]{0.0});
        nodes[8] = (new float[]{12.0f, 55.0f}, new double[]{0.0});

        List<Landmark> landmarks = new();

        Graph smallGraph = new Graph{nodes = nodes, graph = graph, bi_graph = bi_graph, landmarks = landmarks};
        BiDijkstra biDijkstra = new BiDijkstra(smallGraph);
        Dijkstra dijkstra = new Dijkstra(smallGraph);
        var dijkstraResult = dijkstra.FindShortestPath(1, 4);
        var bidijkstraResult = biDijkstra.FindShortestPath(1, 4);
        Assert.AreEqual(dijkstraResult.distance, bidijkstraResult.distance);
    }

}
