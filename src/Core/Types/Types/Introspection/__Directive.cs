using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __Directive
        : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("__Directive");

            descriptor.Description(
                "A Directive provides a way to describe alternate runtime execution and " +
                "type validation behavior in a GraphQL document.\n\n" +
                "In some cases, you need to provide options to alter GraphQL's " +
                "execution behavior in ways field arguments will not suffice, such as " +
                "conditionally including or skipping a field. Directives provide this by " +
                "describing additional information to the executor.");

            descriptor.Field("name")
                .Type<NonNullType<StringType>>()
                .Resolver(c => c.Parent<DirectiveType>().Name);

            descriptor.Field("description")
                .Type<StringType>()
                .Resolver(c => c.Parent<DirectiveType>().Description);

            descriptor.Field("locations")
                .Type<NonNullType<ListType<NonNullType<__DirectiveLocation>>>>()
                .Resolver(c => c.Parent<DirectiveType>().Locations);

            descriptor.Field("args")
                .Type<NonNullType<ListType<NonNullType<__InputValue>>>>()
                .Resolver(c => c.Parent<DirectiveType>().Arguments);

            descriptor.Field("onOperation")
                .Type<NonNullType<BooleanType>>()
                .Resolver(c => GetOnOperation(c))
                .DeprecationReason("Use `locations`.");

            descriptor.Field("onFragment")
                .Type<NonNullType<BooleanType>>()
                .Resolver(c => GetOnFragment(c))
                .DeprecationReason("Use `locations`.");

            descriptor.Field("onField")
                .Type<NonNullType<BooleanType>>()
                .Resolver(c => GetOnField(c))
                .DeprecationReason("Use `locations`.");
        }

        private static bool GetOnOperation(IResolverContext context)
        {
            ICollection<DirectiveLocation> locations =
                context.Parent<DirectiveType>().Locations;

            return locations.Contains(DirectiveLocation.Query)
                || locations.Contains(DirectiveLocation.Mutation)
                || locations.Contains(DirectiveLocation.Subscription);
        }

        private static bool GetOnFragment(IResolverContext context)
        {
            ICollection<DirectiveLocation> locations =
                context.Parent<DirectiveType>().Locations;

            return locations.Contains(DirectiveLocation.InlineFragment)
                || locations.Contains(DirectiveLocation.FragmentSpread)
                || locations.Contains(DirectiveLocation.FragmentDefinition);
        }

        private static bool GetOnField(IResolverContext context)
        {
            ICollection<DirectiveLocation> locations =
                context.Parent<DirectiveType>().Locations;

            return locations.Contains(DirectiveLocation.Field);
        }
    }
}
