namespace HotChocolate.Types.Introspection
{
    internal sealed class __TypeField
        : Field
    {
        internal __TypeField()
            : base(new FieldConfig
            {
                Name = "__type",
                Description = "Request the type information of a single type.",
                Type = t => t.GetType<IOutputType>(typeof(__Type)),
                Arguments = new[]
                {
                    new InputField(new InputFieldConfig
                    {
                        Name ="type",
                        Type = t => new NonNullType(t.GetType<IOutputType>(typeof(StringType)))
                    })
                },
                Resolver = r => (ctx, ct) => ctx.Schema.GetType<INamedType>(ctx.Argument<string>("type"))
            })
        {
        }
    }
}
