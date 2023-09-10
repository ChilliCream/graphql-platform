namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class RegisterDataLoaderInfo : ISyntaxInfo, IEquatable<RegisterDataLoaderInfo>
{
    public RegisterDataLoaderInfo(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public bool Equals(RegisterDataLoaderInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name;
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

        return other is RegisterDataLoaderInfo info && Equals(info);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) ||
            obj is RegisterDataLoaderInfo other && Equals(other);

    public override int GetHashCode()
        => Name.GetHashCode();

    public static bool operator ==(RegisterDataLoaderInfo? left, RegisterDataLoaderInfo? right)
        => Equals(left, right);

    public static bool operator !=(RegisterDataLoaderInfo? left, RegisterDataLoaderInfo? right)
        => !Equals(left, right);
}
