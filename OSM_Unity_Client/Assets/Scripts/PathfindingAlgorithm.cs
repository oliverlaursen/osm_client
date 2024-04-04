using System;
using UnityEngine;

public interface IPathfindingAlgorithm
{
    public (float, long[]) FindShortestPath(long start, long end);
}
