namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class DataLoaderInfo : ISyntaxInfo, IEquatable<DataLoaderInfo>
{
    public DataLoaderInfo(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public bool Equals(DataLoaderInfo? other)
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

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) ||
            obj is DataLoaderInfo other && Equals(other);

    public override int GetHashCode()
        => Name.GetHashCode();

    public static bool operator ==(DataLoaderInfo? left, DataLoaderInfo? right)
        => Equals(left, right);

    public static bool operator !=(DataLoaderInfo? left, DataLoaderInfo? right)
        => !Equals(left, right);
}
