#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace HotChocolate;

public class SemanticNonNullTypeInterceptor : TypeInterceptor
{
    private ITypeInspector _typeInspector = null!;

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
    }

    public override void OnAfterCompleteName(ITypeCompletionContext completionContext, DefinitionBase definition)
    {
        if (completionContext.IsIntrospectionType)
        {
            return;
        }

        if (definition is ObjectTypeDefinition objectDef)
        {
            foreach (var field in objectDef.Fields)
            {
                if (field.IsIntrospectionField)
                {
                    continue;
                }

                ApplySemanticNonNullDirective(field, completionContext);

                field.FormatterDefinitions.Add(CreateSemanticNonNullResultFormatterDefinition());
            }
        }
        else if (definition is InterfaceTypeDefinition interfaceDef)
        {
            foreach (var field in interfaceDef.Fields)
            {
                ApplySemanticNonNullDirective(field, completionContext);
            }
        }
    }

    private void ApplySemanticNonNullDirective(
        OutputFieldDefinitionBase field,
        ITypeCompletionContext completionContext)
    {
        if (!HasNonNullType(field))
        {
            return;
        }

        var directiveDependency = new TypeDependency(
            _typeInspector.GetTypeRef(typeof(SemanticNonNullDirective)),
            TypeDependencyFulfilled.Completed);

        ((RegisteredType)completionContext).Dependencies.Add(directiveDependency);

        field.AddDirective(new SemanticNonNullDirective(), _typeInspector);

        field.Type = RewriteTypeToNullableType(field, _typeInspector);
    }

    private static bool HasNonNullType(OutputFieldDefinitionBase definition)
    {
        var reference = definition.Type;

        if (reference is ExtendedTypeReference extendedTypeRef)
        {
            return !extendedTypeRef.Type.IsNullable;
        }

        if (reference is SchemaTypeReference schemaRef)
        {
            return schemaRef.Type is NonNullType;
        }

        if (reference is SyntaxTypeReference syntaxRef)
        {
            return syntaxRef.Type is NonNullTypeNode;
        }

        return false;
    }

    private static TypeReference RewriteTypeToNullableType(
        OutputFieldDefinitionBase definition,
        ITypeInspector typeInspector)
    {
        var reference = definition.Type;

        if (reference is ExtendedTypeReference extendedTypeRef)
        {
            return extendedTypeRef.Type.IsNullable
                ? extendedTypeRef
                : extendedTypeRef.WithType(
                    typeInspector.ChangeNullability(extendedTypeRef.Type, true));
        }

        if (reference is SchemaTypeReference schemaRef)
        {
            return schemaRef.Type is NonNullType nnt
                ? schemaRef.WithType(nnt.Type)
                : schemaRef;
        }

        if (reference is SyntaxTypeReference syntaxRef)
        {
            return syntaxRef.Type is NonNullTypeNode nnt
                ? syntaxRef.WithType(nnt.Type)
                : syntaxRef;
        }

        throw new NotSupportedException();
    }

    private static ResultFormatterDefinition CreateSemanticNonNullResultFormatterDefinition()
        => new((ctx, result) =>
            {
                if (result is null)
                {
                    throw new GraphQLException(CreateSemanticNonNullViolationError(ctx));
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
}
