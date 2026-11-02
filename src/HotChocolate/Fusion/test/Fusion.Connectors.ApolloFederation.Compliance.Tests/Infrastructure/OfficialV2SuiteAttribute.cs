namespace HotChocolate.Fusion;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class OfficialV2SuiteAttribute(string id) : Attribute
{
    public string Id { get; } = id;

    public NodeResolution NodeResolution { get; set; } = NodeResolution.Gateway;

    public bool AllowNonResolvableInterfaceObjects { get; set; }

    public ShareableFieldRuntimeTypeRouting ShareableFieldRuntimeTypeRouting { get; set; } =
        ShareableFieldRuntimeTypeRouting.SourceLocal;
}
