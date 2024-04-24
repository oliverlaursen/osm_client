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
    public void NewTestScriptSimplePasses()
    {
        var denmarkGraph = MapController.DeserializeGraph("Assets/Maps/denmark.graph");
        long startNode = 280276691;
        long endNode = 896780523;
        double expectedDistance = 324741;

        BiDijkstra biDijkstra = new BiDijkstra(denmarkGraph);
        biDijkstra.FindShortestPath(startNode, endNode);

        // Use the Assert class to test conditions
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
