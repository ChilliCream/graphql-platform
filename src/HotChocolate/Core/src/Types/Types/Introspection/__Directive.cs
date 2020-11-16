#pragma warning disable IDE1006 // Naming Styles
using System.Collections.Generic;
using HotChocolate.Properties;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __Directive : ObjectType
    {
        protected override void Configure(
            IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Name(Names.__Directive)
                .Description(TypeResources.Directive_Description);

            descriptor
                .Field(Names.Name)
                .Type<NonNullType<StringType>>()
                .Resolve(c => c.Parent<DirectiveType>().Name);

            descriptor
                .Field(Names.Description)
                .Type<StringType>()
                .Resolve(c => c.Parent<DirectiveType>().Description);

            descriptor
                .Field(Names.IsRepeatable)
                .Type<BooleanType>()
                .Resolve(c => c.Parent<DirectiveType>().IsRepeatable);

            descriptor
                .Field(Names.Locations)
                .Type<NonNullType<ListType<NonNullType<__DirectiveLocation>>>>()
                .Resolve(c => c.Parent<DirectiveType>().Locations);

            descriptor
                .Field(Names.Args)
                .Type<NonNullType<ListType<NonNullType<__InputValue>>>>()
                .Resolve(c => c.Parent<DirectiveType>().Arguments);

            descriptor
                .Field(Names.OnOperation)
                .Type<NonNullType<BooleanType>>()
                .Resolve(c => GetOnOperation(c))
                .Deprecated(TypeResources.Directive_UseLocation);

            descriptor
                .Field(Names.OnFragment)
                .Type<NonNullType<BooleanType>>()
                .Resolve(c => GetOnFragment(c))
                .Deprecated(TypeResources.Directive_UseLocation);

            descriptor
                .Field(Names.OnField)
                .Type<NonNullType<BooleanType>>()
                .Resolve(c => GetOnField(c))
                .Deprecated(TypeResources.Directive_UseLocation);
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

        public static class Names
        {
            public const string __Directive = "__Directive";
            public const string Name = "name";
            public const string Description = "description";
            public const string IsRepeatable = "isRepeatable";
            public const string Locations = "locations";
            public const string Args = "args";
            public const string OnOperation = "onOperation";
            public const string OnFragment = "onFragment";
            public const string OnField = "onField";
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
