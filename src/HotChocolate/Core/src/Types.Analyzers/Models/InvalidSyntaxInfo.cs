namespace HotChocolate.Types.Analyzers.Models;

/// <summary>
/// Carries a diagnostic for a class that failed inspection. No source is generated for it.
/// </summary>
public sealed class InvalidSyntaxInfo(string key) : SyntaxInfo
{
    public override string OrderByKey { get; } = key;

    public override bool Equals(object? obj)
        => obj is InvalidSyntaxInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? other)
        => other is InvalidSyntaxInfo info && Equals(info);

    private bool Equals(InvalidSyntaxInfo other)
        => string.Equals(OrderByKey, other.OrderByKey, StringComparison.Ordinal);

    public override int GetHashCode()
        => OrderByKey.GetHashCode();
}
