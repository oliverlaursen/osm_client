using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class MultLandmarkHeuristic : AStarHeuristic
{
    private IEnumerable<LandmarkHeuristic> landmarks;

    public MultLandmarkHeuristic(IEnumerable<LandmarkHeuristic> landmarks)
    {
        this.landmarks = landmarks;
    }

    public float Calculate(long node, long end)
    {
        foreach (LandmarkHeuristic landmark in landmarks)
        {
            landmark.Calculate(node, end);
        }
        return landmarks.Select(landmark => landmark.Calculate(node, end)).Max();
    }
}