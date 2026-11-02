using HotChocolate.Fusion.ApolloFederation;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@link</c> directive from Apollo Federation declares the federation vocabulary a source
/// schema imports. It is recognized on source schemas and removed during composition.
/// </summary>
internal sealed class LinkMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public LinkMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(FederationDirectiveNames.Link)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Url,
                new NonNullType(stringType)));
        Arguments.Add(new MutableInputFieldDefinition(ArgumentNames.As, stringType));
        Arguments.Add(new MutableInputFieldDefinition(ArgumentNames.For, stringType));
        Arguments.Add(new MutableInputFieldDefinition(ArgumentNames.Import, new ListType(stringType)));
        IsRepeatable = true;
        Locations = DirectiveLocation.Schema;
    }

    public static LinkMutableDirectiveDefinition Create(ISchemaDefinition schema)
    {
        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(
            SpecScalarNames.String.Name,
            out var stringType))
        {
            stringType = BuiltIns.String.Create();
        }

        return new LinkMutableDirectiveDefinition(stringType);
    }

    private static class ArgumentNames
    {
        public const string As = "as";
        public const string For = "for";
        public const string Import = "import";
    }
}
