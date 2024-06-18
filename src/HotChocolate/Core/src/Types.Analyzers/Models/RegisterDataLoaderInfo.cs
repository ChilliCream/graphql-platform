namespace HotChocolate.Types.Analyzers.Models;

public sealed class RegisterDataLoaderInfo(string name) : ISyntaxInfo
{
    public string Name { get; } = name;

    public override bool Equals(object? obj)
        => obj is RegisterDataLoaderInfo other && Equals(other);

    public bool Equals(ISyntaxInfo other)
        => other is RegisterDataLoaderInfo info && Equals(info);

    private bool Equals(RegisterDataLoaderInfo other)
        => string.Equals(Name, other.Name, StringComparison.Ordinal);

    public override int GetHashCode()
        => HashCode.Combine(Name);
}
