namespace HotChocolate.Fusion.Composition;

public sealed class SubGraphConfiguration
{
    public SubGraphConfiguration(
        string name,
        string schema,
        string extensions)
        : this(name, schema, new[] { extensions }) { }

    public SubGraphConfiguration(
        string name,
        string schema,
        params string[] extensions)
    {
        Name = name;
        Schema = schema;
        Extensions = extensions;
    }

    public string Name { get; }

    public string Schema { get; }

    public IReadOnlyList<string> Extensions { get; }
}
