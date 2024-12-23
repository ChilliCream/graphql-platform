namespace HotChocolate.Types.Analyzers.Models;

public sealed class ModuleInfo(string moduleName, ModuleOptions options) : SyntaxInfo
{
    public string ModuleName { get; } = moduleName;

    public ModuleOptions Options { get; } = options;

    public override string OrderByKey => ModuleName;

    public override bool Equals(object? obj)
        => obj is ModuleInfo other && Equals(other);

    public override bool Equals(SyntaxInfo obj)
        => obj is ModuleInfo other && Equals(other);

    private bool Equals(ModuleInfo other)
        => Options.Equals(other.Options)
            && string.Equals(ModuleName, other.ModuleName, StringComparison.Ordinal);

    public override int GetHashCode()
        => HashCode.Combine(Options, ModuleName);
}
