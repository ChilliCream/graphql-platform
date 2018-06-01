namespace HotChocolate.Types.Introspection
{
    internal sealed class __TypeNameField
        : Field
    {
        internal __TypeNameField()
            : base(new FieldConfig
            {
                Name = "__typename",
                Description = "The name of the current Object type at runtime.",
                Type = t => new NonNullType(t.GetType<IOutputType>(typeof(StringType))),
                Resolver = r => (ctx, ct) => ctx.ObjectType.Name
            })
        {
        }
    }
}
