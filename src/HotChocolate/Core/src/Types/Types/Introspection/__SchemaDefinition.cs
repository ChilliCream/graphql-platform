#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Types.Descriptors.TypeReference;

namespace HotChocolate.Types.Introspection;

[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __SchemaDefinition : UnionType
{
    protected override UnionTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
        => new(Names.__SchemaDefinition)
        {
            ResolveAbstractType = ResolveType,
            Types =
            {
                Create(nameof(__Type)),
                Create(nameof(__Field)),
                Create(nameof(__InputValue)),
                Create(nameof(__EnumValue)),
                Create(nameof(__Directive))
            }
        };

    private static ObjectType? ResolveType(IResolverContext context, object resolverResult)
    {
        var typeName = resolverResult switch
        {
            EnumValue => __EnumValue.Names.__EnumValue,
            DirectiveType => __Directive.Names.__Directive,
            IOutputFieldDefinition => __Field.Names.__Field,
            IInputValueDefinition => __InputValue.Names.__InputValue,
            IType => __Type.Names.__Type,
            _ => null
        };

        if (typeName is not null
            && context.Schema.Types.TryGetType(typeName, out var type)
            && type is ObjectType objectType)
        {
            return objectType;
        }

        return null;
    }

    public static class Names
    {
        // ReSharper disable once InconsistentNaming
        public const string __SchemaDefinition = "__SchemaDefinition";
    }
}
#pragma warning restore IDE1006 // Naming Styles
