using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public UnityEngine.UI.Slider speedSlider;
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
    public GameObject landmarkSquare;

    private List<GameObject> landmarksInstances;
    private GameObject circleAInstance;
    private GameObject circleBInstance;
    private float circleSize = 300f;

    private AStar astar;
    private BiAStar biastar;
    private Dijkstra dijkstra;
    private BiDijkstra bidijkstra;
    private Landmarks landmarks;
    private Landmarks landmarks300;

    private Graph graph;

    public bool visual = true;

    private int drawspeed = 0;

    public void InitializeAlgorithms(Graph graph)
    {
        this.graph = graph;
        astar = new AStar(graph);
        biastar = new BiAStar(graph);
        dijkstra = new Dijkstra(graph);
        bidijkstra = new BiDijkstra(graph);
        landmarks = new Landmarks(graph);
        landmarks300 = new Landmarks(graph, updateLandmarks: 300);
    }

    public void ChangeVisual()
    {
        visual = !visual;
    }

    void Update()
    {
        drawspeed = (int)speedSlider.value;
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
        if (landmarksInstances != null && landmarksInstances.Count > 0)
        {
            foreach (var instance in landmarksInstances)
            {
                instance.transform.localScale = 2 * new Vector3(circleSize * (Camera.main.orthographicSize / maxOrthoSize), circleSize * (Camera.main.orthographicSize / maxOrthoSize), 0);
            }
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
                if (node_selection == 0)
                {
                    MapController.ChangeTextFieldHelper(GameObject.Find("StartField"), closestNode.ToString());
                    SelectNodeA();
                }
                else
                {
                    MapController.ChangeTextFieldHelper(GameObject.Find("EndField"), closestNode.ToString());
                    SelectNodeB();
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                lastPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }

            // If the right mouse button is held down and mouse isnt hovering over UI
            if (Input.GetMouseButton(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                Vector3 delta = lastPosition - Camera.main.ScreenToWorldPoint(Input.mousePosition);
                transform.Translate(delta, Space.World);
                lastPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
        }
    }

    public void SelectNodeA(){
        var node = long.Parse(GameObject.Find("StartField").GetComponent<TMPro.TMP_InputField>().text);
        var nodes = GameObject.Find("Map").GetComponent<MapController>().graph.nodes;
        float[] nodeCoords = nodes[node].Item1;
        nodeA = node;
        Destroy(circleAInstance);
        circleAInstance = Instantiate(circleA, new Vector3(nodeCoords[0], nodeCoords[1], 0), Quaternion.identity);
    }
    public void SelectNodeB(){
        var node = long.Parse(GameObject.Find("EndField").GetComponent<TMPro.TMP_InputField>().text);
        var nodes = GameObject.Find("Map").GetComponent<MapController>().graph.nodes;
        float[] nodeCoords = nodes[node].Item1;
        nodeB = node;
        Destroy(circleBInstance);
        circleBInstance = Instantiate(circleB, new Vector3(nodeCoords[0], nodeCoords[1], 0), Quaternion.identity);
    }

    public void SetNodeSelection(int selection)
    {
        node_selection = selection;
    }

    long ClosestNode(Vector2 position, (float[], double[])[] nodes)
    {
        float minDistance = float.MaxValue;
        long closestNode = 0;
        foreach (var (i,node) in nodes.Select((Value, Index) => (Index, Value)))
        {
            var x = node.Item1[0];
            var y = node.Item1[1];
            var dX = position.x - x;
            var dY = position.y - y;
            var distance = dX * dX + dY * dY;
            if (distance < minDistance)
            {
                minDistance = distance;
                closestNode = i;
            }
        }
        Debug.Log("Edges for " + closestNode + ":");
        for (int i = 0; i < graph.graph[closestNode].Length; i++)
        {
            Debug.Log("Node: " + graph.graph[closestNode][i].node + " Cost: " + graph.graph[closestNode][i].cost);
        }
        Debug.Log("Bi-Edges for " + closestNode + ": ");
        for (int i = 0; i < graph.bi_graph[closestNode].Length; i++)
        {
            Debug.Log("Node: " + graph.bi_graph[closestNode][i].node + " Cost: " + graph.bi_graph[closestNode][i].cost);
        }
        return closestNode;
    }

    public void DijkstraOnSelection(bool bidirectional)
    {
        ClearLandmarks();
        IPathfindingAlgorithm chosen_algo = bidirectional ? bidijkstra : dijkstra;
        ShortestPathAlgoOnSelection(chosen_algo);
    }

    private void ShortestPathAlgoOnSelection(IPathfindingAlgorithm algo)
    {
        StopAllCoroutines();
        var lineRenderer = Camera.main.gameObject.GetComponent<GLLineRenderer>();
        lineRenderer.ClearDiscoveryPath();
        lineRenderer.ClearPath();
        if (visual)
        {
            StartCoroutine(algo.FindShortestPathWithVisual(nodeA, nodeB, drawspeed));
        }
        else
        {
            var result = algo.FindShortestPath(nodeA, nodeB);
            if (result == null) { return; }
            result.DisplayAndDrawPath(graph);
        }
    }

    public void AstarOnSelection(bool bidirectional)
    {
        ClearLandmarks();
        IPathfindingAlgorithm chosen_algo = bidirectional ? biastar : astar;
        ShortestPathAlgoOnSelection(chosen_algo);
    }

    public void LandmarksOnSelection(bool updateLandmarks)
    {
        ClearLandmarks();
        DrawLandmarks();
        IPathfindingAlgorithm algo = updateLandmarks ? landmarks300 : landmarks;
        ShortestPathAlgoOnSelection(algo);
    }

    public void ClearLandmarks()
    {
        if (landmarksInstances != null)
        {
            foreach (var instance in landmarksInstances)
            {
                Destroy(instance);
            }
            landmarksInstances.Clear();

        }
    }

    public void DrawLandmarks()
    {
        var instances = new List<GameObject>();
        for (int i = 0; i < graph.landmarks.Count; i++)
        {
            var landmark = graph.landmarks[i];
            var node = graph.nodes[landmark.node_id];
            var nodeCoords = node.Item1;
            var landmarkInstance = Instantiate(landmarkSquare, new Vector3(nodeCoords[0], nodeCoords[1], 0), Quaternion.identity);
            landmarkInstance.transform.localScale = new Vector3(circleSize * (Camera.main.orthographicSize / maxOrthoSize), circleSize * (Camera.main.orthographicSize / maxOrthoSize), 0);
            landmarkInstance.name = "Landmark " + landmark.node_id;
            instances.Add(landmarkInstance);
        }
        this.landmarksInstances = instances;
    }

    public void FlipNodes()
    {
        if (nodeA == 0 || nodeB == 0) { return; }
        var temp_loc = circleBInstance.transform.position;
        circleBInstance.transform.position = circleAInstance.transform.position;
        circleAInstance.transform.position = temp_loc;
        var temp_node = nodeA;
        nodeA = nodeB;
        nodeB = temp_node;
    }
}