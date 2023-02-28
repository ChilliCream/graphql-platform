namespace HotChocolate.Fusion.Composition;

public sealed class SubGraphConfiguration
{
    public SubGraphConfiguration(string name, string schema)
    {
        Name = name;
        Schema = schema;
    }

    public string Name { get; }

    public string Schema { get; }
}
