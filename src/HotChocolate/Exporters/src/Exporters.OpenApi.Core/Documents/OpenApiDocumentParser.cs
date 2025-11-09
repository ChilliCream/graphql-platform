using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Exporters.OpenApi;

public sealed class OpenApiDocumentParser(ISchemaDefinition schema)
{
    private static readonly ExternalFragmentReferenceFinder s_externalFragmentReferenceFinder = new();

    public OpenApiParseResult Parse(OpenApiDocumentDefinition document)
    {
        var documentNode = document.Document;

        var localFragments = documentNode.Definitions
            .OfType<FragmentDefinitionNode>()
            .ToArray();
        var localFragmentLookup = localFragments
            .ToDictionary(f => f.Name.Value);

        // An operation document can only define a single operation alongside local fragment definitions.
        var operationDefinitions = documentNode.Definitions.OfType<OperationDefinitionNode>().ToArray();

        if (operationDefinitions.Length > 1)
        {
            var error = new OpenApiParsingError(
                "An operation document can only define a single operation alongside local fragment definitions.",
                document.Id,
                documentNode);
            return OpenApiParseResult.Failure(error);
        }

        if (operationDefinitions.Length == 1)
        {
            var operationDefinition = operationDefinitions[0];
            return ParseOperation(document.Id, operationDefinition, documentNode, localFragmentLookup);
        }

        if (localFragments.Length == 0)
        {
            var error = new OpenApiParsingError(
                "Document must contain either a single operation or at least one fragment definition.",
                document.Id,
                documentNode);
            return OpenApiParseResult.Failure(error);
        }

        var fragmentDefinition = localFragments[0];
        localFragmentLookup.Remove(fragmentDefinition.Name.Value);

        return ParseFragment(document.Id, fragmentDefinition, documentNode, localFragmentLookup);
    }

    private OpenApiParseResult ParseFragment(
        string id,
        FragmentDefinitionNode fragment,
        DocumentNode document,
        Dictionary<string, FragmentDefinitionNode> localFragmentLookup)
    {
        var name = fragment.Name.Value;
        var description = fragment.Description?.Value;

        if (!schema.Types.TryGetType(fragment.TypeCondition.Name.Value, out var typeCondition))
        {
            var error = new OpenApiParsingError(
                $"Type condition '{fragment.TypeCondition.Name.Value}' not found in schema.",
                id,
                document);
            return OpenApiParseResult.Failure(error);
        }

        var context = new FragmentSpreadFinderContext(localFragmentLookup);
        s_externalFragmentReferenceFinder.Visit(document, context);

        var fragmentDocument = new OpenApiFragmentDocument(
            id,
            name,
            description,
            typeCondition,
            fragment,
            localFragmentLookup,
            context.ExternalFragmentReferences);

        return OpenApiParseResult.Success(fragmentDocument);
    }

    private static OpenApiParseResult ParseOperation(
        string id,
        OperationDefinitionNode operation,
        DocumentNode document,
        Dictionary<string, FragmentDefinitionNode> localFragmentLookup)
    {
        var name = operation.Name?.Value;
        var description = operation.Description?.Value;

        if (string.IsNullOrEmpty(name))
        {
            var error = new OpenApiParsingError(
                "Operation must have a name.",
                id,
                document);
            return OpenApiParseResult.Failure(error);
        }

        var httpDirective = operation.Directives.FirstOrDefault(d => d.Name.Value == WellKnownDirectiveNames.Http);

        if (httpDirective is null)
        {
            var error = new OpenApiParsingError(
                $"Operation '{name}' must be annotated with @http directive.",
                id,
                document);
            return OpenApiParseResult.Failure(error);
        }

        if (!TryParseHttpMethod(httpDirective, id, name, document, out var httpMethod, out var httpMethodError))
        {
            return OpenApiParseResult.Failure(httpMethodError);
        }

        if (!TryParseRoute(httpDirective, id, name, document, out var route, out var routeError))
        {
            return OpenApiParseResult.Failure(routeError);
        }

        if (!TryParseQueryParameters(httpDirective, id, name, document, out var queryParameters, out var queryParametersError))
        {
            return OpenApiParseResult.Failure(queryParametersError);
        }

        var bodyParameter = GetBodyParameter(operation);

        var cleanOperation = RewriteOperation(operation);
        cleanOperation = cleanOperation
            .WithVariableDefinitions(
                cleanOperation.VariableDefinitions
                    .Select(RewriteVariableDefinition)
                    .ToArray());

        var context = new FragmentSpreadFinderContext(localFragmentLookup);
        s_externalFragmentReferenceFinder.Visit(document, context);

        var operationDocument = new OpenApiOperationDocument(
            id,
            name,
            description,
            httpMethod,
            route,
            queryParameters,
            bodyParameter,
            cleanOperation,
            localFragmentLookup,
            context.ExternalFragmentReferences);

        return OpenApiParseResult.Success(operationDocument);
    }

    private static OpenApiRouteSegmentParameter? GetBodyParameter(OperationDefinitionNode operation)
    {
        var variableWithBodyDirective = operation.VariableDefinitions
            .FirstOrDefault(v => v.Directives
                .Any(d => d.Name.Value == WellKnownDirectiveNames.Body));

        if (variableWithBodyDirective is null)
        {
            return null;
        }

        var variableName = variableWithBodyDirective.Variable.Name.Value;

        return new OpenApiRouteSegmentParameter(variableName, variableName, null);
    }

    private static bool TryParseQueryParameters(
        DirectiveNode httpDirective,
        string documentId,
        string operationName,
        DocumentNode document,
        [NotNullWhen(true)] out ImmutableArray<OpenApiRouteSegmentParameter> queryParameters,
        [NotNullWhen(false)] out OpenApiParsingError? error)
    {
        queryParameters = default;

        var value = httpDirective.Arguments
            .FirstOrDefault(x => x.Name.Value == WellKnownArgumentNames.QueryParameters)?.Value;

        if (value is null)
        {
            queryParameters = [];
            error = null;
            return true;
        }

        if (value is StringValueNode { Value: var singleValue })
        {
            if (!TryParseParameter(singleValue, documentId, operationName, document, out var parameter, out error))
            {
                return false;
            }

            queryParameters = [parameter];
            error = null;
            return true;
        }

        if (value is not ListValueNode listValue)
        {
            error = new OpenApiParsingError(
                $"Query parameters argument on @http directive for operation '{operationName}' must be a list of strings.",
                documentId,
                document);
            return false;
        }

        var builder = ImmutableArray.CreateBuilder<OpenApiRouteSegmentParameter>(listValue.Items.Count);

        foreach (var item in listValue.Items)
        {
            if (item is not StringValueNode { Value: var stringValue })
            {
                error = new OpenApiParsingError(
                    $"Query parameters argument on @http directive for operation '{operationName}' must contain only string values.",
                    documentId,
                    document);
                return false;
            }

            if (!TryParseParameter(stringValue, documentId, operationName, document, out var parameter, out error))
            {
                return false;
            }

            builder.Add(parameter);
        }

        queryParameters = builder.MoveToImmutable();
        error = null;
        return true;
    }

    private static bool TryParseRoute(
        DirectiveNode httpDirective,
        string documentId,
        string operationName,
        DocumentNode document,
        [NotNullWhen(true)] out OpenApiRoute? route,
        [NotNullWhen(false)] out OpenApiParsingError? error)
    {
        route = null;

        var routeArgument = httpDirective.Arguments
            .FirstOrDefault(x => x.Name.Value == WellKnownArgumentNames.Route);

        if (routeArgument is null)
        {
            error = new OpenApiParsingError(
                $"@http directive for operation '{operationName}' must have a 'route' argument.",
                documentId,
                document);
            return false;
        }

        var value = routeArgument.Value;

        if (value is not StringValueNode { Value: var stringValue } || string.IsNullOrEmpty(stringValue))
        {
            error = new OpenApiParsingError(
                $"Route argument on @http directive for operation '{operationName}' must be a non-empty string.",
                documentId,
                document);
            return false;
        }

        var segments = stringValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var builder = ImmutableArray.CreateBuilder<IOpenApiRouteSegment>(segments.Length);

        foreach (var segment in segments)
        {
            if (segment.StartsWith("{") && segment.EndsWith("}"))
            {
                var parameter = segment.Substring(1, segment.Length - 2);
                if (!TryParseParameter(parameter, documentId, operationName, document, out var parsedParameter, out error))
                {
                    return false;
                }

                builder.Add(parsedParameter);
            }
            else
            {
                builder.Add(new OpenApiRouteSegmentLiteral(segment));
            }
        }

        route = new OpenApiRoute(builder.MoveToImmutable());
        error = null;
        return true;
    }

    private static bool TryParseParameter(
        string parameter,
        string documentId,
        string operationName,
        DocumentNode document,
        [NotNullWhen(true)] out OpenApiRouteSegmentParameter? parsedParameter,
        [NotNullWhen(false)] out OpenApiParsingError? error)
    {
        parsedParameter = null;

        var span = parameter.AsSpan();
        var colonIndex = span.IndexOf(':');

        if (colonIndex == -1)
        {
            parsedParameter = new OpenApiRouteSegmentParameter(parameter, parameter, null);
            error = null;
            return true;
        }

        if (colonIndex + 1 >= span.Length || span[colonIndex + 1] != '$')
        {
            error = new OpenApiParsingError(
                $"Explicit route segment variable mappings must start with '$', got '{parameter}' in operation '{operationName}'.",
                documentId,
                document);
            return false;
        }

        var key = span[..colonIndex].ToString();

        // Skip ':$'
        var mappingSyntax = span[(colonIndex + 2)..];

        var firstDotIndex = mappingSyntax.IndexOf('.');

        if (firstDotIndex == -1)
        {
            parsedParameter = new OpenApiRouteSegmentParameter(key, mappingSyntax.ToString(), null);
            error = null;
            return true;
        }

        var variable = mappingSyntax[..firstDotIndex].ToString();
        var pathSpan = mappingSyntax[(firstDotIndex + 1)..];

        var segmentCount = 1;
        for (var i = 0; i < pathSpan.Length; i++)
        {
            if (pathSpan[i] == '.')
            {
                segmentCount++;
            }
        }

        var builder = ImmutableArray.CreateBuilder<string>(segmentCount);

        while (!pathSpan.IsEmpty)
        {
            var dotIndex = pathSpan.IndexOf('.');
            var segment = dotIndex == -1
                ? pathSpan
                : pathSpan[..dotIndex];

            builder.Add(segment.ToString());

            pathSpan = dotIndex == -1
                ? ReadOnlySpan<char>.Empty
                : pathSpan[(dotIndex + 1)..];
        }

        parsedParameter = new OpenApiRouteSegmentParameter(key, variable, builder.MoveToImmutable());
        error = null;
        return true;
    }

    private static bool TryParseHttpMethod(
        DirectiveNode httpDirective,
        string documentId,
        string operationName,
        DocumentNode document,
        [NotNullWhen(true)] out string? httpMethod,
        [NotNullWhen(false)] out OpenApiParsingError? error)
    {
        httpMethod = null;

        var methodArgument = httpDirective.Arguments
            .FirstOrDefault(x => x.Name.Value == WellKnownArgumentNames.Method);

        if (methodArgument is null)
        {
            error = new OpenApiParsingError(
                $"@http directive for operation '{operationName}' must have a 'method' argument.",
                documentId,
                document);
            return false;
        }

        var value = methodArgument.Value;

        if (value is not EnumValueNode { Value: var stringValue } || string.IsNullOrEmpty(stringValue))
        {
            error = new OpenApiParsingError(
                $"Method argument on @http directive for operation '{operationName}' must be a non-empty enum value.",
                documentId,
                document);
            return false;
        }

        if (stringValue != HttpMethods.Get
            // TODO: Might not even want to support that
#if NET10_0_OR_GREATER
            && stringValue != HttpMethods.Query
#endif
            && stringValue != HttpMethods.Post
            && stringValue != HttpMethods.Put
            && stringValue != HttpMethods.Patch
            && stringValue != HttpMethods.Delete)
        {
            error = new OpenApiParsingError(
                $"Invalid HTTP method value '{stringValue}' on @http directive for operation '{operationName}'.",
                documentId,
                document);
            return false;
        }

        httpMethod = stringValue;
        error = null;
        return true;
    }

    private static OperationDefinitionNode RewriteOperation(OperationDefinitionNode operation)
    {
        if (operation.Directives.Count == 0)
        {
            return operation;
        }

        var directives = operation.Directives
            .Where(v => v.Name.Value != WellKnownDirectiveNames.Http)
            .ToArray();

        return directives.Length == operation.Directives.Count
            ? operation
            : operation.WithDirectives(directives);
    }

    private static VariableDefinitionNode RewriteVariableDefinition(VariableDefinitionNode variable)
    {
        if (variable.Directives.Count == 0)
        {
            return variable;
        }

        var directives = variable.Directives
            .Where(v => v.Name.Value != WellKnownDirectiveNames.Body)
            .ToArray();

        return directives.Length == variable.Directives.Count
            ? variable
            : variable.WithDirectives(directives);
    }

    private sealed class ExternalFragmentReferenceFinder : SyntaxVisitor<FragmentSpreadFinderContext>
    {
        protected override ISyntaxVisitorAction Enter(ISyntaxNode node, FragmentSpreadFinderContext context)
        {
            if (node is FragmentSpreadNode fragmentSpread)
            {
                var fragmentName = fragmentSpread.Name.Value;
                if (!context.LocalFragmentLookup.ContainsKey(fragmentName))
                {
                    context.ExternalFragmentReferences.Add(fragmentName);
                }
            }

            return Continue;
        }
    }

    private sealed class FragmentSpreadFinderContext(
        Dictionary<string, FragmentDefinitionNode> localFragmentLookup)
    {
        public HashSet<string> ExternalFragmentReferences { get; } = [];

        public Dictionary<string, FragmentDefinitionNode> LocalFragmentLookup { get; } = localFragmentLookup;
    }
}
