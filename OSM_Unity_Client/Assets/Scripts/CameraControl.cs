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
    public long nodeA;
    public long nodeB;
    public GameObject circleA;
    public GameObject circleB;

    private GameObject circleAInstance;
    private GameObject circleBInstance;
    private float circleSize = 300f;

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
                float[] nodeCoords = nodes[closestNode];
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
                Debug.Log(closestNode);
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

    long ClosestNode(Vector2 position, Dictionary<long, float[]> nodes)
    {
        float minDistance = float.MaxValue;
        long closestNode = 0;
        foreach (var node in nodes)
        {
            var x = node.Value[0];
            var y = node.Value[1];
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
        var graph = GameObject.Find("Map").GetComponent<MapController>().graph;
        var (distance, path) = GameObject.Find("Map").GetComponent<MapController>().Dijkstra(graph, nodeA, nodeB);
        var lineRenderer = Camera.main.gameObject.GetComponent<GLLineRenderer>();
        lineRenderer.ClearPath();
        GameObject.Find("Map").GetComponent<MapController>().DrawPath(graph.nodes, path);
        Debug.Log("Distance: " + distance);
    }

}