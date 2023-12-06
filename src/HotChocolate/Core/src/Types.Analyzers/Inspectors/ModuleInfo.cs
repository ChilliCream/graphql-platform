namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class ModuleInfo : ISyntaxInfo, IEquatable<ModuleInfo>
{
    public ModuleInfo(string moduleName, ModuleOptions options)
    {
        ModuleName = moduleName;
        Options = options;
    }

    public string ModuleName { get; }

    public ModuleOptions Options { get; }

    public bool Equals(ModuleInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ModuleName == other.ModuleName && Options == other.Options;
    }
    
    public bool Equals(ISyntaxInfo other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is ModuleInfo info && Equals(info);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) ||
            (obj is ModuleInfo other && Equals(other));

    public override int GetHashCode()
    {
        unchecked
        {
            return (ModuleName.GetHashCode() * 397) ^ (int)Options;
        }
    }
}
