#pragma warning disable IDE1006 // Naming Styles
using System.Linq;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __Schema : ObjectType<ISchema>
    {
        protected override void Configure(IObjectTypeDescriptor<ISchema> descriptor)
        {
            descriptor
                .Name(Names.__Schema)
                .Description(TypeResources.Schema_Description)
                // Introspection types must always be bound explicitly so that we
                // do not get any interference with conventions.
                .BindFields(BindingBehavior.Explicit);

            descriptor
                .Field(Names.Description)
                .Type<StringType>()
                .Resolver(c => c.Schema.Description);

            descriptor
                .Field(Names.Types)
                .Description(TypeResources.Schema_Types)
                .Type<NonNullType<ListType<NonNullType<__Type>>>>()
                .Resolver(c => c.Schema.Types);

            descriptor
                .Field(t => t.QueryType)
                .Name(Names.QueryType)
                .Description(TypeResources.Schema_QueryType)
                .Type<NonNullType<__Type>>();

            descriptor
                .Field(t => t.MutationType)
                .Name(Names.MutationType)
                .Description(TypeResources.Schema_MutationType)
                .Type<__Type>();

            descriptor
                .Field(t => t.SubscriptionType)
                .Name(Names.SubscriptionType)
                .Description(TypeResources.Schema_SubscriptionType)
                .Type<__Type>();

            descriptor
                .Field(Names.Directives)
                .Description(TypeResources.Schema_Directives)
                .Type<NonNullType<ListType<NonNullType<__Directive>>>>()
                .Resolver(c => c.Schema.DirectiveTypes);

            if (descriptor.Extend().Context.Options.EnableDirectiveIntrospection)
            {
                descriptor
                    .Field(t => t.Directives.Where(d => d.Type.IsPublic).Select(d => d.ToNode()))
                    .Type<NonNullType<ListType<NonNullType<__AppliedDirective>>>>()
                    .Name(Names.AppliedDirectives);
            }
        }

        public static class Names
        {
            public const string __Schema = "__Schema";
            public const string Description = "description";
            public const string Types = "types";
            public const string QueryType = "queryType";
            public const string MutationType = "mutationType";
            public const string SubscriptionType = "subscriptionType";
            public const string Directives = "directives";
            public const string AppliedDirectives = "appliedDirectives";
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
