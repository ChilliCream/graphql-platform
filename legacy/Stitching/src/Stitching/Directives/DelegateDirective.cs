namespace HotChocolate.Stitching;

public sealed class DelegateDirective
{
    public DelegateDirective(string? path, string schema)
    {
        Path = path;
        Schema = schema;
    }

    public string? Path { get; }

    public string Schema { get; }
}
