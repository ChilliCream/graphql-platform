using HotChocolate.Configuration;
using HotChocolate.OpenApi.Exceptions;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.OpenApi.OpenApiResources;

namespace HotChocolate.OpenApi.TypeInterceptors;

internal sealed class AbstractResolverTypeInterceptor : TypeInterceptor
{
    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (completionContext is not { IsIntrospectionType: false, IsDirective: false })
        {
            return;
        }

        switch (definition)
        {
            case UnionTypeDefinition type:
                MapUnionTypeResolver(type);
                break;
        }
    }

    private static void MapUnionTypeResolver(UnionTypeDefinition definition)
    {
        IReadOnlyDictionary<string, ObjectType>? types = null;

        definition.ResolveAbstractType = (context, _) =>
        {
            var fieldType = context.Selection.Field.Type.NamedType();

            if (types is null && fieldType is UnionType unionType)
            {
                types = unionType.Types;
            }

            if (types is null)
            {
                throw new InvalidOperationException();
            }

            if (fieldType.ContextData.TryGetValue(
                    WellKnownContextData.OpenApiTypeMap,
                    out var typeMapValue) &&
                typeMapValue is Dictionary<string, string> typeMap &&
                context.ContextData.TryGetValue(
                    WellKnownContextData.OpenApiHttpStatusCode,
                    out var httpStatusCodeValue) &&
                httpStatusCodeValue is string httpStatusCode &&
                types.TryGetValue(
                    GetTypeNameByHttpStatusCode(typeMap, httpStatusCode),
                    out var type))
            {
                return type;
            }

            throw new InvalidOperationException();
        };
    }

    private static string GetTypeNameByHttpStatusCode(
        IReadOnlyDictionary<string, string> typeMap,
        string httpStatusCode)
    {
        // Direct match (200 = 200).
        if (typeMap.TryGetValue(httpStatusCode, out var typeName1))
        {
            return typeName1;
        }

        // Wildcard match (200 = 2XX).
        if (typeMap.TryGetValue(httpStatusCode[0] + "XX", out var typeName2))
        {
            return typeName2;
        }

        // Default match (200 = default).
        if (typeMap.TryGetValue("default", out var typeName3))
        {
            return typeName3;
        }

        throw new AbstractResolverException(
            string.Format(
                AbstractResolverTypeInterceptor_UnableToGetTypeNameForStatusCode,
                httpStatusCode));
    }
}
