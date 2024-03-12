using System.Collections.Generic;
using System.Linq;
using ProjNet.CoordinateSystems;
using UnityEditor.UI;
using UnityEngine;

public class AstarNode
{
    public long NodeId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float GCost { get; set; }
    public float HCost { get; set; }
    public float FCost { get; set; }
    public AstarNode Parent { get; set; } //parent node in the path

    public AstarNode(long nodeId, float x, float y, float gCost, float hCost, float fCost, AstarNode parent)
    {
        NodeId = nodeId;
        X = x;
        Y = y;
        GCost = gCost;
        HCost = hCost;
        FCost = fCost;
        Parent = parent;
    }
}

public class AStar
{
    public Graph graph;
    private SortedSet<AstarNode> openlist = new SortedSet<AstarNode>(new NodeComparer()); //priority queue
    private HashSet<long> closedList = new HashSet<long>();

    public AStar(Graph graph)
    {
        this.graph = graph;
    }

   public (float, long[]) FindShortestPath(long start, long end)
{
    Debug.Log("A*");

    float[] startCoords = graph.nodes[start];

    AstarNode startNode = new AstarNode(start, startCoords[0], startCoords[1], 0, 0, 0, null);
    openlist.Add(startNode);

    int nodesVisited = 0;
    while (openlist.Count > 0)
    {
        AstarNode currentNode = openlist.Min;
        if (currentNode.NodeId == end)
        {
            Debug.Log("nodes visited " + nodesVisited);
            float[] node = graph.nodes[currentNode.NodeId];
            // Reconstruct path
            return (currentNode.GCost, ReconstructPath(currentNode));
        }
        openlist.Remove(currentNode);
        closedList.Add(currentNode.NodeId); // Mark current node as visited
        nodesVisited++;

        Edge[] neighbors = graph.GetNeighbors(currentNode.NodeId);

        foreach (Edge neighbor in neighbors)
        {

            // Cost from start to this node
            float gCost = neighbor.cost + currentNode.GCost;
            float hCost = HeuristicCostEstimate(neighbor.node, end);
            float fCost = gCost + hCost;

            AstarNode openNode = openlist.FirstOrDefault(node => node.NodeId == neighbor.node);

            if (openNode != null && openNode.FCost < fCost)
            {
                continue;
            }

            // Check if neighbor has already been visited
            if (closedList.Contains(neighbor.node))
            {
                continue;
            }

            float[] graphNode = graph.nodes[neighbor.node];
            AstarNode neighborNode = new AstarNode(neighbor.node, graphNode[0], graphNode[1], gCost, hCost, fCost, currentNode);
            openlist.Add(neighborNode);
        }
    }

    return (0, new long[0]);
}



    private float HeuristicCostEstimate(long start, long end)
    {
        // Implement your heuristic here. This could be Manhattan, Euclidean, etc.
        // For now, let's assume it's Euclidean distance.
        var startCoords = graph.nodes[start];
        var endCoords = graph.nodes[end];
        var distance = Mathf.Sqrt(Mathf.Pow(endCoords[0] - startCoords[0], 2) + Mathf.Pow(endCoords[1] - startCoords[1], 2));

        if (distance <= 0)
        {
            UnityEngine.Debug.Log("distance is negative");
        }
        return distance;
    }

    private long[] ReconstructPath(AstarNode EndNode)
    {
        var path = new List<long>();
        var parent = EndNode;


        // Reconstruct the path
        while (parent != null)
        {
            path.Add(parent.NodeId);
            parent = parent.Parent;
        }

        path.Reverse();
        return path.ToArray();
    }

    private class NodeComparer : IComparer<AstarNode>
    {
        public int Compare(AstarNode x, AstarNode y)
        {
            if (x.FCost == y.FCost)
                return 0;
            else if (x.FCost < y.FCost)
                return -1;
            else
                return 1;
        }
    }
}