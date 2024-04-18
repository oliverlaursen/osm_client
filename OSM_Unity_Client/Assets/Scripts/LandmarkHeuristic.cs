public class LandmarkHeuristic : AStarHeuristic
{
    private Landmark landmark;

    public LandmarkHeuristic(Landmark landmark)
    {
        this.landmark = landmark;
    }

    public float Calculate(long node, long end)
    {
        var a = landmark.distances[end];
        var b = landmark.distances[node];
        return a - b;
    }
}