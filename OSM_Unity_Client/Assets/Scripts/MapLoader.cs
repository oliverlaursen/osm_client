using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
    public class Node
    {
        public string id;
        public float x;
        public float y;
    }

    [System.Serializable]
    public class Nodes
    {
        public List<Node> nodes;
    }

    [System.Serializable]
    public class Way
    {
        public string id;
        public List<string> node_refs;
    }

    [System.Serializable]
    public class Ways
    {
        public List<Way> ways;
    }

public class MapLoader : MonoBehaviour
{
    public TextAsset mapFile;
    
    void Start(){
        Nodes nodes = JsonUtility.FromJson<Nodes>(mapFile.text);
        Ways ways = JsonUtility.FromJson<Ways>(mapFile.text);
        Debug.Log(ways.ways.Count);
        foreach(Way way in ways.ways){
            // Draw lines between nodes
            for(int i = 0; i < way.node_refs.Count - 1; i++){
                Node node1 = nodes.nodes.Find(x => x.id == way.node_refs[i]);
                Node node2 = nodes.nodes.Find(x => x.id == way.node_refs[i+1]);
                Vector3 pos1 = new Vector3(node1.x, node1.y, 0);
                Vector3 pos2 = new Vector3(node2.x, node2.y, 0);
                Debug.DrawLine(pos1, pos2, Color.red, 10000f);
            }
        }
    }
}
