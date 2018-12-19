namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __TypeNameField
        : ObjectField
    {
        internal __TypeNameField()
           : base(IntrospectionFields.TypeName, d =>
           {
               d.Description("The name of the current Object type at runtime.")
                   .Type<NonNullType<StringType>>()
                   .Resolver(ctx => ctx.ObjectType.Name.Value);
           })
        {
        }

        public override bool IsIntrospectionField { get; } = true;
    }
}
