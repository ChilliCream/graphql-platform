namespace HotChocolate.Types.Analyzers.Models;

public sealed class DataLoaderModuleInfo(string moduleName, bool isInternal) : SyntaxInfo
{
    public string ModuleName { get; } = moduleName;

    public bool IsInternal { get; } = isInternal;

    public override string OrderByKey => ModuleName;

    public override bool Equals(object? obj)
        => obj is DataLoaderModuleInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? obj)
        => obj is DataLoaderModuleInfo other && Equals(other);

    private bool Equals(DataLoaderModuleInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return OrderByKey.Equals(other.OrderByKey, StringComparison.Ordinal)
            && IsInternal == other.IsInternal;
    }

    public override int GetHashCode()
        => HashCode.Combine(OrderByKey, IsInternal);
}
