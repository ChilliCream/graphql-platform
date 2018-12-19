using HotChocolate.Configuration;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __Schema
        : ObjectType<ISchema>
    {
        protected override void Configure(IObjectTypeDescriptor<ISchema> descriptor)
        {
            descriptor.Name("__Schema");

            descriptor.Description(
                "A GraphQL Schema defines the capabilities of a GraphQL server. It " +
                "exposes all available types and directives on the server, as well as " +
                "the entry points for query, mutation, and subscription operations.");

            descriptor.BindFields(BindingBehavior.Explicit);

            descriptor.Field("types")
                .Description("A list of all types supported by this server.")
                .Type<NonNullType<ListType<NonNullType<__Type>>>>()
                .Resolver(c => c.Schema.Types);

            descriptor.Field(t => t.QueryType)
                .Description("The type that query operations will be rooted at.")
                .Type<NonNullType<__Type>>();

            descriptor.Field(t => t.MutationType)
                .Description("If this server supports mutation, the type that " +
                    "mutation operations will be rooted at.")
                .Type<__Type>();

            descriptor.Field(t => t.SubscriptionType)
                .Description("If this server support subscription, the type that " +
                    "subscription operations will be rooted at.")
                .Type<__Type>();

            descriptor.Field("directives")
                .Description("A list of all directives supported by this server.")
                .Type<NonNullType<ListType<NonNullType<__Directive>>>>()
                .Resolver(c => c.Schema.DirectiveTypes);
        }
    }
}
