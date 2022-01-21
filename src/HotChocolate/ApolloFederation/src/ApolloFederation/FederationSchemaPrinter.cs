using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities.Introspection;

namespace HotChocolate.ApolloFederation;

public static partial class FederationSchemaPrinter
{
    public static string Print(ISchema schema)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        return SerializeSchema(schema).ToString();
    }

    private static DocumentNode SerializeSchema(
        ISchema schema)
    {
        var referenced = new ReferencedTypes();
        var definitionNodes = new List<IDefinitionNode>();

        foreach (INamedType namedType in GetRelevantTypes(schema))
        {
            if (TrySerializeType(namedType, referenced, out IDefinitionNode? definitionNode))
            {
                definitionNodes.Add(definitionNode);
            }
        }

        return new DocumentNode(null, definitionNodes);
    }

    private static IEnumerable<INamedType> GetRelevantTypes(ISchema schema)
        => schema.Types
            .Where(IncludeType)
            .OrderBy(t => t.Name.Value, StringComparer.Ordinal);

    private static bool TrySerializeType(
        INamedType namedType,
        ReferencedTypes referenced,
        [NotNullWhen(true)] out IDefinitionNode? definitionNode)
    {
        definitionNode = namedType switch
        {
            ObjectType type => SerializeObjectType(type, referenced),
            InterfaceType type => SerializeInterfaceType(type, referenced),
            InputObjectType type => SerializeInputObjectType(type, referenced),
            UnionType type => SerializeUnionType(type, referenced),
            EnumType type => SerializeEnumType(type, referenced),
            ScalarType type => SerializeScalarType(type, referenced),
            _ => throw new NotSupportedException()
        };
        return definitionNode is not null;
    }

    private static ITypeNode SerializeType(
        IType type,
        ReferencedTypes referenced)
    {
        return type switch
        {
            NonNullType nt => new NonNullTypeNode(
                (INullableTypeNode)SerializeType(nt.Type, referenced)),
            ListType lt => new ListTypeNode(SerializeType(lt.ElementType, referenced)),
            INamedType namedType => SerializeNamedType(namedType, referenced),
            _ => throw new NotSupportedException()
        };
    }

    private static NamedTypeNode SerializeNamedType(
        INamedType namedType,
        ReferencedTypes referenced)
    {
        referenced.TypeNames.Add(namedType.Name);
        return new NamedTypeNode(null, new NameNode(namedType.Name));
    }

    private static DirectiveNode SerializeDirective(
        IDirective directiveType,
        ReferencedTypes referenced)
    {
        referenced.DirectiveNames.Add(directiveType.Name);
        return directiveType.ToNode(true);
    }

    private static StringValueNode? SerializeDescription(string? description)
        => description is { Length: > 0 }
            ? new StringValueNode(description)
            : null;

    private static bool IncludeType(INamedType type)
        => !IsBuiltInType(type) &&
           !IsApolloFederationType(type);

    private static bool IncludeField(IOutputField field)
        => !field.IsIntrospectionField &&
           !IsApolloFederationType(field.Type.NamedType());

    private static bool IsApolloFederationType(INamedType type)
        => type is EntityType or ServiceType ||
           type.Name.Equals(WellKnownTypeNames.Any) ||
           type.Name.Equals(WellKnownTypeNames.FieldSet);

    private static bool IsBuiltInType(INamedType type) =>
        IntrospectionTypes.IsIntrospectionType(type.Name) ||
        BuiltInTypes.IsBuiltInType(type.Name);

    private sealed class ReferencedTypes
    {
        public HashSet<string> TypeNames { get; } = new();
        public HashSet<string> DirectiveNames { get; } = new();
    }
}
