namespace HotChocolate.Types.Analyzers.Inspectors;

internal sealed class SyntaxInfoComparer : IEqualityComparer<ISyntaxInfo>
{
    public bool Equals(ISyntaxInfo? x, ISyntaxInfo? y)
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

    public int GetHashCode(ISyntaxInfo obj)
        => obj.GetHashCode();
    
    public static SyntaxInfoComparer Default { get; } = new();
}