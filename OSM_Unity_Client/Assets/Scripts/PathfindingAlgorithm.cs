using System;
using System.Collections;
using UnityEngine;

public interface IPathfindingAlgorithm
{
    public void FindShortestPath(long start, long end);

    public IEnumerator FindShortestPathWithVisual(long start, long end);
}
