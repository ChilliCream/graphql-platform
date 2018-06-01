namespace HotChocolate.Types.Introspection
{
    internal sealed class __SchemaField
        : Field
    {
        internal __SchemaField()
            : base(new FieldConfig
            {
                Name = "__schema",
                Description = "Access the current type schema of this server.",
                Type = t => new NonNullType(t.GetType<IOutputType>(typeof(__Schema))),
                Resolver = r => (ctx, ct) => ctx.Schema
            })
        {
        }
    }
}
