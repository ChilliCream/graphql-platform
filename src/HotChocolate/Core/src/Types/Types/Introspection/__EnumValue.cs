#pragma warning disable IDE1006 // Naming Styles
using System.Linq;
using HotChocolate.Properties;

#nullable enable
namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __EnumValue : ObjectType<IEnumValue>
    {
        protected override void Configure(
            IObjectTypeDescriptor<IEnumValue> descriptor)
        {
            descriptor
                .Name(Names.__EnumValue)
                .Description(TypeResources.EnumValue_Description)
                // Introspection types must always be bound explicitly so that we
                // do not get any interference with conventions.
                .BindFields(BindingBehavior.Explicit);

            descriptor
                .Field(c => c.Name)
                .Name(Names.Name)
                .Type<NonNullType<StringType>>();

            descriptor
                .Field(c => c.Description)
                .Name(Names.Description);

            descriptor
                .Field(c => c.IsDeprecated)
                .Name(Names.IsDeprecated)
                .Type<NonNullType<BooleanType>>();

            descriptor
                .Field(c => c.DeprecationReason)
                .Name(Names.DeprecationReason);

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
            public const string __EnumValue = "__EnumValue";
            public const string Name = "name";
            public const string Description = "description";
            public const string IsDeprecated = "isDeprecated";
            public const string DeprecationReason = "deprecationReason";
            public const string AppliedDirectives = "appliedDirectives";
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
