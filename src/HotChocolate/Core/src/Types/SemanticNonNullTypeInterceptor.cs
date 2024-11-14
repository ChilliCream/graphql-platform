#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Types.Relay;

namespace HotChocolate;

public class SemanticNonNullTypeInterceptor : TypeInterceptor
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

    public override void OnAfterCompleteName(ITypeCompletionContext completionContext, DefinitionBase definition)
    {
        if (completionContext.IsIntrospectionType)
        {
            return;
        }

        if (definition is ObjectTypeDefinition objectDef)
        {
            if (((RegisteredType)completionContext).IsMutationType == true)
            {
                // Fields on the Mutation type should stay non-null to abort execution.
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

        while(true)
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

        while(true)
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
            return extendedTypeRef.WithType(typeInspector.ChangeNullability(extendedTypeRef.Type, _fullNullablePattern));
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
        => new((ctx, result) =>
            {
                if (levels.Contains(0) && result is null)
                {
                    throw new GraphQLException(CreateSemanticNonNullViolationError(ctx));
                }

                if (result is IEnumerable<object?> listResult)
                {
                    var index = 0;
                    foreach(var item in listResult)
                    {
                        if (item is null && levels.Contains(1))
                        {
                            var path = ctx.Path.Append(index);
                            var error = CreateSemanticNonNullViolationError(path);

                            ctx.ReportError(error);
                        }

                        index++;
                    }
                }

                return result;
            },
            key: WellKnownMiddleware.SemanticNonNull,
            isRepeatable: false);

    private static IError CreateSemanticNonNullViolationError(IResolverContext context)
        => ErrorBuilder.New()
            .SetMessage("TODO")
            .AddLocation(context.Selection.SyntaxNode)
            .SetPath(context.Path)
            .Build();

    private static IError CreateSemanticNonNullViolationError(Path path)
        => ErrorBuilder.New()
            .SetMessage("TODO")
            .SetPath(path)
            .Build();
}
