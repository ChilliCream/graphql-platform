namespace HotChocolate.Stitching;

public sealed class SourceDirective
{
    public SourceDirective(string name, string schema)
    {
        Name = name;
        Schema = schema;
    }

    public string Name { get; }

    public string Schema { get; }
}
