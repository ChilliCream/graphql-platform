namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class AggregateInfo : ISyntaxInfo, IEquatable<AggregateInfo>
{
    private readonly IReadOnlyList<ISyntaxInfo> _syntaxInfos;

    public AggregateInfo(IReadOnlyList<ISyntaxInfo> syntaxInfos)
    {
        _syntaxInfos = syntaxInfos;
    }

    public IReadOnlyList<ISyntaxInfo> Items => _syntaxInfos;

    public bool Equals(AggregateInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return _syntaxInfos == other._syntaxInfos;
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) ||
            obj is AggregateInfo other && Equals(other);

    public override int GetHashCode()
        => _syntaxInfos.GetHashCode();

    public static bool operator ==(AggregateInfo? left, AggregateInfo? right)
        => Equals(left, right);

    public static bool operator !=(AggregateInfo? left, AggregateInfo? right)
        => !Equals(left, right);
}