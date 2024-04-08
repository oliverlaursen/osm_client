using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float zoomSpeed = 1000f;
    public float minOrthoSize = 0.1f;
    public float maxOrthoSize = float.MaxValue;
    private float mouseSensitivity = 1f;
    private Vector3 lastPosition;
    public int node_selection = 0;
    public long nodeA = 0;
    public long nodeB = 0;
    public GameObject circleA;
    public GameObject circleB;

    private GameObject circleAInstance;
    private GameObject circleBInstance;
    private float circleSize = 300f;

    private AStar astar;
    private Dijkstra dijkstra;
    private BiDijkstra bidijkstra;
    
    private Graph graph;

    public bool visual = true;
    private bool bidirectional = true;

    public void InitializeAlgorithms(Graph graph)
    {
        this.graph = graph;
        astar = new AStar(graph);
        dijkstra = new Dijkstra(graph);
        bidijkstra = new BiDijkstra(graph);
    }

    public void ChangeVisual()
    {
        visual = !visual;
    }

    public void ChangeBidirectional(){
        bidirectional = !bidirectional;
    }



    void Update()
    {
        circleSize = (float)(maxOrthoSize * 0.05);
        if (circleAInstance != null)
        {
            // Scale the circle to the camera size
            circleAInstance.transform.localScale = new Vector3(circleSize * (Camera.main.orthographicSize / maxOrthoSize), circleSize * (Camera.main.orthographicSize / maxOrthoSize), 0);
        }
        if (circleBInstance != null)
        {
            circleBInstance.transform.localScale = new Vector3(circleSize * (Camera.main.orthographicSize / maxOrthoSize), circleSize * (Camera.main.orthographicSize / maxOrthoSize), 0);
        }
        zoomSpeed = Camera.main.orthographicSize * 0.4f;
        // Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        Camera.main.orthographicSize -= scroll * zoomSpeed;
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minOrthoSize, maxOrthoSize);
        mouseSensitivity = Camera.main.orthographicSize / maxOrthoSize;
        {
            if (Input.GetMouseButtonDown(1))
            {
                var clickPosition = Input.mousePosition;
                var worldPosition = Camera.main.ScreenToWorldPoint(clickPosition);
                var nodes = GameObject.Find("Map").GetComponent<MapController>().graph.nodes;
                long closestNode = ClosestNode(worldPosition, nodes);
                float[] nodeCoords = nodes[closestNode].Item1;
                var lineRenderer = GetComponent<GLLineRenderer>();
                if (node_selection == 0)
                {
                    nodeA = closestNode;
                    Destroy(circleAInstance);
                    circleAInstance = Instantiate(circleA, new Vector3(nodeCoords[0], nodeCoords[1], 0), Quaternion.identity);
                }
                else
                {
                    nodeB = closestNode;
                    Destroy(circleBInstance);
                    circleBInstance = Instantiate(circleB, new Vector3(nodeCoords[0], nodeCoords[1], 0), Quaternion.identity);
                }
                var node_print = "Node: " + closestNode;
                // Print node edges
                var graph = GameObject.Find("Map").GetComponent<MapController>().graph.graph;
                var edges = graph[closestNode];
                foreach (var edge in edges)
                {
                    var endNode = edge.node;
                    var edge_print = "Edge: " + endNode;
                    node_print += "\n" + edge_print;
                }
                Debug.Log(node_print);
            }

            if (Input.GetMouseButtonDown(0))
            {
                lastPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }

            if (Input.GetMouseButton(0))
            {
                Vector3 delta = lastPosition - Camera.main.ScreenToWorldPoint(Input.mousePosition);
                transform.Translate(delta, Space.World);
                lastPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
        }
    }

    public void SetNodeSelection(int selection)
    {
        node_selection = selection;
    }

    long ClosestNode(Vector2 position, Dictionary<long, (float[],double[])> nodes)
    {
        float minDistance = float.MaxValue;
        long closestNode = 0;
        foreach (var node in nodes)
        {
            var x = node.Value.Item1[0];
            var y = node.Value.Item1[1];
            var dX = position.x - x;
            var dY = position.y - y;
            var distance = dX*dX + dY*dY;
            if (distance < minDistance)
            {
                minDistance = distance;
                closestNode = node.Key;
            }
        }
        return closestNode;
    }

    public void DijkstraOnSelection()
    {

        StopAllCoroutines();
        var lineRenderer = Camera.main.gameObject.GetComponent<GLLineRenderer>();
        lineRenderer.ClearDiscoveryPath();
        lineRenderer.ClearPath();
        IPathfindingAlgorithm algo = bidirectional ? bidijkstra : dijkstra;
        if (visual) {
            StartCoroutine(algo.FindShortestPathWithVisual(nodeA, nodeB));
        }
        else
        {
            algo.FindShortestPath(nodeA, nodeB);
        }
    }



    public void AstarOnSelection()
    {
        StopAllCoroutines();
        var lineRenderer = Camera.main.gameObject.GetComponent<GLLineRenderer>();
        lineRenderer.ClearDiscoveryPath();
        lineRenderer.ClearPath();
        if (visual) {
            StartCoroutine(astar.FindShortestPathWithVisual(nodeA, nodeB));
        }
        else
        {
            astar.FindShortestPath(nodeA, nodeB);
        }
    }

    public void FlipNodes()
    {
        if(nodeA == 0 || nodeB == 0) { return; }
        var temp_loc = circleBInstance.transform.position;
        circleBInstance.transform.position = circleAInstance.transform.position;
        circleAInstance.transform.position = temp_loc;
        var temp_node = nodeA;
        nodeA = nodeB;
        nodeB = temp_node;
    }
}