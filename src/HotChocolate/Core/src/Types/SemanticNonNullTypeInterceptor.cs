#nullable enable

using System.Collections;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;

namespace HotChocolate;

internal sealed class SemanticNonNullTypeInterceptor : TypeInterceptor
{
    private ITypeInspector _typeInspector = null!;
    private ObjectTypeConfiguration? _mutationDef;

    public override bool IsEnabled(IDescriptorContext context)
        => context.Options.EnableSemanticNonNull;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _typeInspector = context.TypeInspector;
    }

    public override void OnAfterResolveRootType(ITypeCompletionContext completionContext, ObjectTypeConfiguration configuration,
        OperationType operationType)
    {
        if (operationType is OperationType.Mutation)
        {
            _mutationDef = configuration;
        }
    }

    public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, TypeSystemConfiguration configuration)
    {
        if (completionContext.IsIntrospectionType)
        {
            return;
        }

        if (configuration is ObjectTypeConfiguration objectDef)
        {
            if (objectDef.Name is "CollectionSegmentInfo" or "PageInfo")
            {
                return;
            }

            // We undo semantic non-nullability on each mutation field, since mutations can be chained
            // and we want to retain the null-bubbling so execution is aborted if a non-null mutation field
            // produces an error.
            if (objectDef == _mutationDef)
            {
                return;
            }

            foreach (var field in objectDef.Fields)
            {
                if (field.IsIntrospectionField)
                {
                    continue;
                }

                if (field.Name == "id")
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

                field.FormatterConfigurations.Add(CreateSemanticNonNullResultFormatterConfiguration(levels));
            }
        }
        else if (configuration is InterfaceTypeConfiguration interfaceDef)
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

                if (field.Name == "id")
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
        OutputFieldConfiguration field,
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
                currentType = nonNullType.NullableType;
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

    private static readonly bool?[] s_fullNullablePattern = Enumerable.Range(0, 32).Select(_ => (bool?)true).ToArray();

    private static TypeReference BuildNullableTypeStructure(
        TypeReference typeReference,
        ITypeInspector typeInspector)
    {
        if (typeReference is ExtendedTypeReference extendedTypeRef)
        {
            return extendedTypeRef.WithType(typeInspector.ChangeNullability(extendedTypeRef.Type,
                s_fullNullablePattern));
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
            return BuildNullableTypeStructure(nonNullType.NullableType);
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

    private static ResultFormatterConfiguration CreateSemanticNonNullResultFormatterConfiguration(HashSet<int> levels)
        => new((context, result) =>
            {
                CheckResultForSemanticNonNullViolations(result, context, context.Path, levels, 0);

                return result;
            },
            isRepeatable: false,
            key: WellKnownMiddleware.SemanticNonNull);

    private static void CheckResultForSemanticNonNullViolations(object? result, IResolverContext context, Path path,
        HashSet<int> levels,
        int currentLevel)
    {
        if (result is null && levels.Contains(currentLevel))
        {
            context.ReportError(CreateSemanticNonNullViolationError(path));
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

    private static IError CreateSemanticNonNullViolationError(Path path)
        => ErrorBuilder.New()
            .SetMessage("Cannot return null for semantic non-null field.")
            .SetCode(ErrorCodes.Execution.SemanticNonNullViolation)
            .SetPath(path)
            .Build();
}
