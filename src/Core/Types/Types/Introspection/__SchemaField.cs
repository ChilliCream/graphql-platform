namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __SchemaField
        : ObjectField
    {
        internal __SchemaField()
            : base(IntrospectionFields.Schema, d =>
            {
                d.Description("Access the current type schema of this server.")
                    .Type<NonNullType<__Schema>>()
                    .Resolver(ctx => ctx.Schema);
            })
        {
        }

        public override bool IsIntrospectionField { get; } = true;
    }
}
