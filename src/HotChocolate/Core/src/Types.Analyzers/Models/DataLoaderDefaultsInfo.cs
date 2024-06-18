namespace HotChocolate.Types.Analyzers.Models;

public sealed class DataLoaderDefaultsInfo(
    bool? scoped,
    bool? isPublic,
    bool? isInterfacePublic,
    bool registerServices)
    : ISyntaxInfo
{
    public bool? Scoped { get; } = scoped;

    public bool? IsPublic { get; } = isPublic;

    public bool? IsInterfacePublic { get; } = isInterfacePublic;

    public bool RegisterServices { get; } = registerServices;

    public override bool Equals(object? obj)
        => obj is DataLoaderDefaultsInfo other && Equals(other);

    public bool Equals(ISyntaxInfo other)
        => other is DataLoaderDefaultsInfo info && Equals(info);

    private bool Equals(DataLoaderDefaultsInfo other)
        => Scoped.Equals(other.Scoped)
            && IsPublic.Equals(other.IsPublic)
            && IsInterfacePublic.Equals(other.IsInterfacePublic)
            && RegisterServices.Equals(other.RegisterServices);

    public override int GetHashCode()
        => HashCode.Combine(Scoped, IsPublic, IsInterfacePublic, RegisterServices);
}
