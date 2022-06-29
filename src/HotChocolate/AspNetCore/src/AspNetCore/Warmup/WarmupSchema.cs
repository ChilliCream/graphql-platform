namespace HotChocolate.AspNetCore.Warmup;

internal class WarmupSchema
{
    public WarmupSchema(string schemaName)
    {
        SchemaName = schemaName;
    }

    public string SchemaName { get; }
}
