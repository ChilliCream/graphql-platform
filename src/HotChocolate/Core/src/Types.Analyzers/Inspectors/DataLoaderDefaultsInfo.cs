namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class DataLoaderDefaultsInfo : ISyntaxInfo, IEquatable<DataLoaderDefaultsInfo>
{
    public DataLoaderDefaultsInfo(
        bool? scoped,
        bool? isPublic,
        bool? isInterfacePublic,
        bool registerServices)
    {
        Scoped = scoped;
        IsPublic = isPublic;
        IsInterfacePublic = isInterfacePublic;
        RegisterServices = registerServices;
    }

    public bool? Scoped { get; }

    public bool? IsPublic { get; }

    public bool? IsInterfacePublic { get; }

    public bool RegisterServices { get; }

    public bool Equals(DataLoaderDefaultsInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Scoped == other.Scoped &&
            IsPublic == other.IsPublic &&
            RegisterServices == other.RegisterServices;
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

        return other is DataLoaderDefaultsInfo info && Equals(info);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) ||
            (obj is ModuleInfo other && Equals(other));

    public override int GetHashCode()
    {
        unchecked
        {
            return (Scoped.GetHashCode() * 397) ^
                IsPublic.GetHashCode() ^
                RegisterServices.GetHashCode();
        }
    }
}
