#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __Field : ObjectType<IOutputField>
    {
        protected override void Configure(
            IObjectTypeDescriptor<IOutputField> descriptor)
        {
            descriptor
                .Name(Names.__Field)
                .Description(TypeResources.Field_Description)
                // Introspection types must always be bound explicitly so that we
                // do not get any interference with conventions.
                .BindFields(BindingBehavior.Explicit);

            descriptor
                .Field(t => t.Name)
                .Name(Names.Name)
                .Type<NonNullType<StringType>>();

            descriptor
                .Field(t => t.Description)
                .Name(Names.Description);

            descriptor
                .Field(t => t.Arguments)
                .Name(Names.Args)
                .Type<NonNullType<ListType<NonNullType<__InputValue>>>>()
                .Resolver(c => c.Parent<IOutputField>().Arguments);

            descriptor
                .Field(t => t.Type)
                .Name(Names.Type)
                .Type<NonNullType<__Type>>();

            descriptor
                .Field(t => t.IsDeprecated)
                .Name(Names.IsDeprecated)
                .Type<NonNullType<BooleanType>>();

            descriptor
                .Field(t => t.DeprecationReason)
                .Name(Names.DeprecationReason);
        }

        public static class Names
        {
            public const string __Field = "__Field";
            public const string Name = "name";
            public const string Description = "description";
            public const string Args = "args";
            public const string Type = "type";
            public const string IsDeprecated = "isDeprecated";
            public const string DeprecationReason = "deprecationReason";
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
