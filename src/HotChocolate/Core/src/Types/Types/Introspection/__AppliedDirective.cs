#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection;

/// <summary>
/// An Applied Directive is an instances of a directive as applied to a schema element.
/// This type is NOT specified by the graphql specification presently.
/// </summary>
[Introspection]
internal sealed class __AppliedDirective : ObjectType<DirectiveNode>
{
    protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        SyntaxTypeReference nonNullStringType = Parse($"{ScalarNames.String}!");
        SyntaxTypeReference locationListType = Parse($"[{nameof(__DirectiveArgument)}!]!");

        return new ObjectTypeDefinition(
            Names.__AppliedDirective,
            TypeResources.___AppliedDirective_Description,
            typeof(DirectiveNode))
        {
            Fields =
                {
                    new(Names.Name, type: nonNullStringType, pureResolver: Resolvers.Name),
                    new(Names.Args, type: locationListType, pureResolver: Resolvers.Arguments)
                }
        };
    }

    private static class Resolvers
    {
        public static string Name(IPureResolverContext context)
            => context.Parent<DirectiveNode>().Name.Value;

        public static object Arguments(IPureResolverContext context)
            => context.Parent<DirectiveNode>().Arguments;
    }

    public static class Names
    {
        public const string __AppliedDirective = "__AppliedDirective";
        public const string Name = "name";
        public const string Args = "args";
    }
}
#pragma warning restore IDE1006 // Naming Styles
