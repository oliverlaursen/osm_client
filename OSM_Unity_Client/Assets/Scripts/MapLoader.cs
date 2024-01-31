using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Node
{
    public string id;
    public double lat;
    public double lon;
}

[System.Serializable]
public class Way
{
    public string id;
    public long[] node_refs;
}

[System.Serializable]
public class PreprocessedOSM
{
    public List<Node> nodes;
    public List<Way> ways;
}

public class MapLoader : MonoBehaviour
{
    public TextAsset mapFile;

    
}


