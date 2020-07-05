using HotChocolate.Properties;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
#pragma warning disable IDE1006 // Naming Styles
    internal sealed class __Schema
#pragma warning restore IDE1006 // Naming Styles
        : ObjectType<ISchema>
    {
        protected override void Configure(IObjectTypeDescriptor<ISchema> descriptor)
        {
            descriptor
                .Name("__Schema")
                .Description(TypeResources.Schema_Description)
                .BindFields(BindingBehavior.Explicit);

            descriptor.Field("description")
                .Type<StringType>()
                .Resolver(c => c.Schema.Description);

            descriptor.Field("types")
                .Description(TypeResources.Schema_Types)
                .Type<NonNullType<ListType<NonNullType<__Type>>>>()
                .Resolver(c => c.Schema.Types);

            descriptor.Field(t => t.QueryType)
                .Description(TypeResources.Schema_QueryType)
                .Type<NonNullType<__Type>>();

            descriptor.Field(t => t.MutationType)
                .Description(TypeResources.Schema_MutationType)
                .Type<__Type>();

            descriptor.Field(t => t.SubscriptionType)
                .Description(TypeResources.Schema_SubscriptionType)
                .Type<__Type>();

            descriptor.Field("directives")
                .Description(TypeResources.Schema_Directives)
                .Type<NonNullType<ListType<NonNullType<__Directive>>>>()
                .Resolver(c => c.Schema.DirectiveTypes);
        }
    }
}
