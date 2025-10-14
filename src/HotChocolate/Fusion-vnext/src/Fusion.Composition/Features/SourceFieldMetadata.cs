namespace HotChocolate.Fusion.Features;

internal sealed class SourceFieldMetadata
{
    public bool HasShareableDirective { get; set; }

    public bool IsExternal { get; set; }

    public bool IsInternal { get; set; }

    public bool IsKeyField { get; set; }

    public bool IsOverridden { get; set; }

    public bool IsShareable { get; set; }
}
