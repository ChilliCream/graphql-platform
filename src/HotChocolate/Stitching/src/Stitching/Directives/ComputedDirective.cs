namespace HotChocolate.Stitching;

public sealed class ComputedDirective
{
    public ComputedDirective(IReadOnlyList<string>? dependantOn)
    {
        DependantOn = dependantOn;
    }

    public IReadOnlyList<string>? DependantOn { get; }
}
