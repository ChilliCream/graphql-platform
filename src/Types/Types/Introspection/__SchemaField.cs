namespace HotChocolate.Types.Introspection
{
    internal sealed class __SchemaField
        : ObjectField
    {
        internal __SchemaField()
            : base("__schema", d =>
            {
                d.Description("Access the current type schema of this server.")
                    .Type<NonNullType<__Schema>>()
                    .Resolver(ctx => ctx.Schema);
            })
        {
        }
    }
}
