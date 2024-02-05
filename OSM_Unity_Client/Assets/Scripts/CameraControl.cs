using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float zoomSpeed = 1000f;
    public float minOrthoSize = 20f;
    public float maxOrthoSize = 320f;
    private float mouseSensitivity = 1f;
    private Vector3 lastPosition;
    public int node_selection = 0;
    public long nodeA;
    public long nodeB;

    void Update()
    {
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
                    lineRenderer.ClearCircle1();
                    lineRenderer.AddCircle1(new Vector3(nodeCoords[0], nodeCoords[1], 0));
                }
                else
                {
                    nodeB = closestNode;
                    lineRenderer.ClearCircle2();
                    lineRenderer.AddCircle2(new Vector3(nodeCoords[0], nodeCoords[1], 0));
                }
                Debug.Log(closestNode);
            }

            if (Input.GetMouseButtonDown(0))
            {
                lastPosition = Input.mousePosition;
            }

            if (Input.GetMouseButton(0))
            {
                var delta = Input.mousePosition - lastPosition;
                transform.Translate(-delta.x * mouseSensitivity, -delta.y * mouseSensitivity, 0);
                lastPosition = Input.mousePosition;
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
            var distance = Vector2.Distance(position, new Vector2(node.Value[0], node.Value[1]));
            if (distance < minDistance)
            {
                minDistance = distance;
                closestNode = node.Key;
            }
        }
        return closestNode;
    }

    public void DijkstraOnSelection(){
        var graph = GameObject.Find("Map").GetComponent<MapController>().graph;
        var (distance, path) = GameObject.Find("Map").GetComponent<MapController>().Dijkstra(graph, nodeA, nodeB);
        var lineRenderer = Camera.main.gameObject.GetComponent<GLLineRenderer>();
        lineRenderer.ClearPath();
        GameObject.Find("Map").GetComponent<MapController>().DrawPath(graph.nodes, path);
        Debug.Log("Distance: " + distance);
    }
}