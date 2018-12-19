namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __DirectiveLocation
        : EnumType<DirectiveLocation>
    {
        protected override void Configure(IEnumTypeDescriptor<DirectiveLocation> descriptor)
        {
            descriptor.Name("__DirectiveLocation");

            descriptor.Description(
                "A Directive can be adjacent to many parts of the GraphQL language, a " +
                "__DirectiveLocation describes one such possible adjacencies.");

            descriptor.Item(DirectiveLocation.Query)
                .Description("Location adjacent to a query operation.");

            descriptor.Item(DirectiveLocation.Mutation)
                .Description("Location adjacent to a mutation operation.");

            descriptor.Item(DirectiveLocation.Subscription)
                .Description("Location adjacent to a subscription operation.");

            descriptor.Item(DirectiveLocation.Field)
                .Description("Location adjacent to a field.");

            descriptor.Item(DirectiveLocation.FragmentDefinition)
                .Name("FRAGMENT_DEFINITION")
                .Description("Location adjacent to a fragment definition.");

            descriptor.Item(DirectiveLocation.FragmentSpread)
                .Name("FRAGMENT_SPREAD")
                .Description("Location adjacent to a fragment spread.");

            descriptor.Item(DirectiveLocation.InlineFragment)
                .Name("INLINE_FRAGMENT")
                .Description("Location adjacent to an inline fragment.");

            descriptor.Item(DirectiveLocation.Schema)
                .Description("Location adjacent to a schema definition.");

            descriptor.Item(DirectiveLocation.Scalar)
                .Description("Location adjacent to a scalar definition.");

            descriptor.Item(DirectiveLocation.Object)
                .Description("Location adjacent to an object type definition.");

            descriptor.Item(DirectiveLocation.FieldDefinition)
                .Name("FIELD_DEFINITION")
                .Description("Location adjacent to a field definition.");

            descriptor.Item(DirectiveLocation.ArgumentDefinition)
                .Name("ARGUMENT_DEFINITION")
                .Description("Location adjacent to an argument definition");

            descriptor.Item(DirectiveLocation.Interface)
                .Description("Location adjacent to an interface definition.");

            descriptor.Item(DirectiveLocation.Union)
                .Description("Location adjacent to a union definition.");

            descriptor.Item(DirectiveLocation.Enum)
                .Description("Location adjacent to an enum definition.");

            descriptor.Item(DirectiveLocation.EnumValue)
                .Name("ENUM_VALUE")
                .Description("Location adjacent to an enum value definition.");

            descriptor.Item(DirectiveLocation.InputObject)
                .Name("INPUT_OBJECT")
                .Description("Location adjacent to an input object type definition.");

            descriptor.Item(DirectiveLocation.InputFieldDefinition)
                .Name("INPUT_FIELD_DEFINITION")
                .Description("Location adjacent to an input object field definition.");
        }
    }
}
