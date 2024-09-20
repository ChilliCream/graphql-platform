namespace HotChocolate.Fusion.Shared.Reviews2;

public class Viewer
{
    public SomeData Data { get; } = new();

    public Review? LatestReview([Service] ReviewRepository repository)
        => repository.GetReview(1);
}
