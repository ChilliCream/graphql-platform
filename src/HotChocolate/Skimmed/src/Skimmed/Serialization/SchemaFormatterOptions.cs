namespace HotChocolate.Skimmed.Serialization;

public struct SchemaFormatterOptions
{
    public bool? OrderByName { get; set; }

    public bool? Indented { get; set; }

    public bool? PrintSpecScalars { get; set; }

    public bool? PrintSpecDirectives { get; set; }
}
