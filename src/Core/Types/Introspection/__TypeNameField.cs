namespace HotChocolate.Types.Introspection
{
    internal sealed class __TypeNameField
        : ObjectField
    {
        internal __TypeNameField()
           : base("__typename", d =>
           {
               d.Description("The name of the current Object type at runtime.")
                   .Type<NonNullType<StringType>>()
                   .Resolver(ctx => ctx.ObjectType.Name);
           })
        {
        }
    }
}
