#pragma warning disable IDE1006 // Naming Styles
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __InputValue : ObjectType<IInputField>
    {
        protected override void Configure(IObjectTypeDescriptor<IInputField> descriptor)
        {
            descriptor
                .Name(Names.__InputValue)
                .Description(TypeResources.InputValue_Description)
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
                .Field(t => t.Type)
                .Name(Names.Type)
                .Type<NonNullType<__Type>>();

            descriptor
                .Field(t => t.DefaultValue)
                .Name(Names.DefaultValue)
                .Description(TypeResources.InputValue_DefaultValue)
                .Type<StringType>()
                .Resolver(c =>
                {
                    IInputField field = c.Parent<IInputField>();
                    if (field.DefaultValue.IsNull())
                    {
                        return null;
                    }

                    return field.DefaultValue?.Print();
                });

            if (descriptor.Extend().Context.Options.EnableDirectiveIntrospection)
            {
                descriptor
                    .Field(t => t.Directives.Select(d => d.ToNode()))
                    .Type<NonNullType<ListType<NonNullType<__AppliedDirective>>>>()
                    .Name(Names.AppliedDirectives);
            }
        }

        public static class Names
        {
            public const string __InputValue = "__InputValue";
            public const string Name = "name";
            public const string Description = "description";
            public const string DefaultValue = "defaultValue";
            public const string Type = "type";
            public const string AppliedDirectives = "appliedDirectives";
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
