namespace HotChocolate.Fusion.Types.Metadata;

public sealed record SourceSchemaInfo(
    string Key,
    string Name,
    string? ConnectorKind)
{
    public SourceSchemaInfo(
        string key,
        string name,
        string? connectorKind,
        bool allowNonResolvableInterfaceObjects)
        : this(key, name, connectorKind)
    {
        AllowNonResolvableInterfaceObjects = allowNonResolvableInterfaceObjects;
    }

    public bool AllowNonResolvableInterfaceObjects { get; init; }
}
