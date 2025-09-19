namespace HotChocolate.Types.Analyzers.Models;

internal sealed class SyntaxInfoComparer : IEqualityComparer<SyntaxInfo>
{
    public bool Equals(SyntaxInfo? x, SyntaxInfo? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.Equals(y);
    }

    public int GetHashCode(SyntaxInfo obj)
        => obj.GetHashCode();

    public static SyntaxInfoComparer Default { get; } = new();
}
