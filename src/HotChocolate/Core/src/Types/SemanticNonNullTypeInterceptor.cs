#nullable enable

using System.Collections;
using HotChocolate.Configuration;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Types.Relay;

namespace HotChocolate;

internal sealed class SemanticNonNullTypeInterceptor : TypeInterceptor
{
    private ITypeInspector _typeInspector = null!;
    private ExtendedTypeReference _nodeTypeReference = null!;

    internal override bool IsEnabled(IDescriptorContext context)
        => context.Options.EnableSemanticNonNull;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _typeInspector = context.TypeInspector;

        _nodeTypeReference = _typeInspector.GetTypeRef(typeof(NodeType));
    }

    /// <summary>
    /// After the root types have been resolved, we go through all the fields of the mutation type
    /// and undo semantic non-nullability. This is because mutations can be chained and we want to retain
    /// the null-bubbling so execution is aborted once one non-null mutation field produces an error.
    /// We have to do this in a different hook because the mutation type is not yet fully resolved in the
    /// <see cref="OnAfterCompleteName"/> hook.
    /// </summary>
    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition,
        OperationType operationType)
    {
        if (operationType == OperationType.Mutation)
        {
            foreach (var field in definition.Fields)
            {
                if (field.IsIntrospectionField)
                {
                    continue;
                }

                if (!field.HasDirectives)
                {
                    continue;
                }

                var semanticNonNullDirective =
                    field.Directives.FirstOrDefault(d => d.Value is SemanticNonNullDirective);

                if (semanticNonNullDirective is not null)
                {
                    field.Directives.Remove(semanticNonNullDirective);
                }

                var semanticNonNullFormatterDefinition =
                    field.FormatterDefinitions.FirstOrDefault(fd => fd.Key == WellKnownMiddleware.SemanticNonNull);

                if (semanticNonNullFormatterDefinition is not null)
                {
                    field.FormatterDefinitions.Remove(semanticNonNullFormatterDefinition);
                }
            }
        }
    }

    public override void OnAfterCompleteName(ITypeCompletionContext completionContext, DefinitionBase definition)
    {
        if (completionContext.IsIntrospectionType)
        {
            return;
        }

        if (definition is ObjectTypeDefinition objectDef)
        {
            if (objectDef.Name is "CollectionSegmentInfo" or "PageInfo")
            {
                return;
            }

            var implementsNode = objectDef.Interfaces.Any(i => i.Equals(_nodeTypeReference));

            foreach (var field in objectDef.Fields)
            {
                if (field.IsIntrospectionField)
                {
                    continue;
                }

                if (implementsNode && field.Name == "id")
                {
                    continue;
                }

                if (field.Type is null)
                {
                    continue;
                }

                var levels = GetSemanticNonNullLevels(field.Type);

                if (levels.Count < 1)
                {
                    continue;
                }

                ApplySemanticNonNullDirective(field, completionContext, levels);

                field.FormatterDefinitions.Add(CreateSemanticNonNullResultFormatterDefinition(levels));
            }
        }
        else if (definition is InterfaceTypeDefinition interfaceDef)
        {
            if (interfaceDef.Name == "Node")
            {
                // The Node interface is well defined, so we don't want to go and change the type of its fields.
                return;
            }

            foreach (var field in interfaceDef.Fields)
            {
                if (field.Type is null)
                {
                    continue;
                }

                var levels = GetSemanticNonNullLevels(field.Type);

                if (levels.Count < 1)
                {
                    continue;
                }

                ApplySemanticNonNullDirective(field, completionContext, levels);
            }
        }
    }

    private void ApplySemanticNonNullDirective(
        OutputFieldDefinitionBase field,
        ITypeCompletionContext completionContext,
        HashSet<int> levels)
    {
        var directiveDependency = new TypeDependency(
            _typeInspector.GetTypeRef(typeof(SemanticNonNullDirective)),
            TypeDependencyFulfilled.Completed);

        ((RegisteredType)completionContext).Dependencies.Add(directiveDependency);

        field.AddDirective(new SemanticNonNullDirective(levels.ToList()), _typeInspector);

        field.Type = BuildNullableTypeStructure(field.Type!, _typeInspector);
    }

    private static HashSet<int> GetSemanticNonNullLevels(TypeReference typeReference)
    {
        if (typeReference is ExtendedTypeReference extendedTypeReference)
        {
            return GetSemanticNonNullLevelsFromReference(extendedTypeReference);
        }

        if (typeReference is SchemaTypeReference schemaRef)
        {
            return GetSemanticNonNullLevelsFromReference(schemaRef);
        }

        if (typeReference is SyntaxTypeReference syntaxRef)
        {
            return GetSemanticNonNullLevelsFromReference(syntaxRef);
        }

        return [];
    }

    private static HashSet<int> GetSemanticNonNullLevelsFromReference(ExtendedTypeReference typeReference)
    {
        var levels = new HashSet<int>();

        var currentType = typeReference.Type;
        var index = 0;

        do
        {
            if (currentType.IsArrayOrList)
            {
                if (!currentType.IsNullable)
                {
                    levels.Add(index);
                }

                index++;
                currentType = currentType.ElementType;
            }
            else if (!currentType.IsNullable)
            {
                levels.Add(index);
                break;
            }
            else
            {
                break;
            }
        } while (currentType is not null);

        return levels;
    }

    private static HashSet<int> GetSemanticNonNullLevelsFromReference(SchemaTypeReference typeReference)
    {
        var levels = new HashSet<int>();

        var currentType = typeReference.Type;
        var index = 0;

        while (true)
        {
            if (currentType is ListType listType)
            {
                index++;
                currentType = listType.ElementType;
            }
            else if (currentType is NonNullType nonNullType)
            {
                levels.Add(index);
                currentType = nonNullType.Type;
            }
            else
            {
                break;
            }
        }

        return levels;
    }

    private static HashSet<int> GetSemanticNonNullLevelsFromReference(SyntaxTypeReference typeReference)
    {
        var levels = new HashSet<int>();

        var currentType = typeReference.Type;
        var index = 0;

        while (true)
        {
            if (currentType is ListTypeNode listType)
            {
                index++;
                currentType = listType.Type;
            }
            else if (currentType is NonNullTypeNode nonNullType)
            {
                levels.Add(index);
                currentType = nonNullType.Type;
            }
            else
            {
                break;
            }
        }

        return levels;
    }

    private static readonly bool?[] _fullNullablePattern = Enumerable.Range(0, 32).Select(_ => (bool?)true).ToArray();

    private static TypeReference BuildNullableTypeStructure(
        TypeReference typeReference,
        ITypeInspector typeInspector)
    {
        if (typeReference is ExtendedTypeReference extendedTypeRef)
        {
            return extendedTypeRef.WithType(typeInspector.ChangeNullability(extendedTypeRef.Type,
                _fullNullablePattern));
        }

        if (typeReference is SchemaTypeReference schemaRef)
        {
            return schemaRef.WithType(BuildNullableTypeStructure(schemaRef.Type));
        }

        if (typeReference is SyntaxTypeReference syntaxRef)
        {
            return syntaxRef.WithType(BuildNullableTypeStructure(syntaxRef.Type));
        }

        throw new NotSupportedException();
    }

    private static IType BuildNullableTypeStructure(ITypeSystemMember typeSystemMember)
    {
        if (typeSystemMember is ListType listType)
        {
            return new ListType(BuildNullableTypeStructure(listType.ElementType));
        }

        if (typeSystemMember is NonNullType nonNullType)
        {
            return BuildNullableTypeStructure(nonNullType.Type);
        }

        return (IType)typeSystemMember;
    }

    private static ITypeNode BuildNullableTypeStructure(ITypeNode typeNode)
    {
        if (typeNode is ListTypeNode listType)
        {
            return new ListTypeNode(BuildNullableTypeStructure(listType.Type));
        }

        if (typeNode is NonNullTypeNode nonNullType)
        {
            return BuildNullableTypeStructure(nonNullType.Type);
        }

        return typeNode;
    }

    private static ResultFormatterDefinition CreateSemanticNonNullResultFormatterDefinition(HashSet<int> levels)
        => new((context, result) =>
            {
                CheckResultForSemanticNonNullViolations(result, context, context.Path, levels, 0);

                return result;
            },
            key: WellKnownMiddleware.SemanticNonNull,
            isRepeatable: false);

    private static void CheckResultForSemanticNonNullViolations(object? result, IResolverContext context, Path path,
        HashSet<int> levels,
        int currentLevel)
    {
        if (result is null && levels.Contains(currentLevel))
        {
            context.ReportError(CreateSemanticNonNullViolationError(path, context.Selection));
            return;
        }

        if (result is IEnumerable enumerable)
        {
            if (currentLevel >= 32)
            {
                // We bail if we're at a depth of 32 as this would mean that we're dealing with an AnyType or another structure.
                return;
            }

            var index = 0;
            foreach (var item in enumerable)
            {
                CheckResultForSemanticNonNullViolations(item, context, path.Append(index), levels, currentLevel + 1);

                index++;
            }
        }
    }

    // TODO: Move
    public static IError CreateSemanticNonNullViolationError(Path path, ISelection selection)
        => ErrorBuilder.New()
            .SetMessage("Cannot return null for semantic non-null field.")
            .SetCode(ErrorCodes.Execution.SemanticNonNullViolation)
            .AddLocation(selection.SyntaxNode)
            .SetPath(path)
            .Build();
}
