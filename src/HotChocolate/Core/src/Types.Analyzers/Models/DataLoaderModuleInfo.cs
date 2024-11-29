namespace HotChocolate.Types.Analyzers.Models;

public sealed class DataLoaderModuleInfo(string moduleName) : SyntaxInfo
{
    public string ModuleName { get; } = moduleName;

    public override string OrderByKey => ModuleName;

    public override bool Equals(object? obj)
        => obj is DataLoaderModuleInfo other && Equals(other);

    public override bool Equals(SyntaxInfo obj)
        => obj is DataLoaderModuleInfo other && Equals(other);

    private bool Equals(DataLoaderModuleInfo other)
        => string.Equals(ModuleName, other.ModuleName, StringComparison.Ordinal);

    public override int GetHashCode()
        => HashCode.Combine(ModuleName);
}
