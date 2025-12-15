using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Adapters.OpenApi;

public static class OpenApiDocumentParser
{
    public static OpenApiDocumentParsingResult Parse(DocumentNode document)
    {
        // An operation document can only define a single operation alongside local fragment definitions.
        var operationDefinitions = document.Definitions.OfType<OperationDefinitionNode>().ToArray();

        if (operationDefinitions.Length > 1)
        {
            var error = new OpenApiDocumentParsingError(
                "An operation document can only define a single operation alongside local fragment definitions.",
                document);
            return OpenApiDocumentParsingResult.Failure(error);
        }

        if (operationDefinitions.Length == 1)
        {
            var operationDefinition = operationDefinitions[0];
            return ParseOperation(operationDefinition, document);
        }

        var fragments = document.Definitions
            .OfType<FragmentDefinitionNode>()
            .ToArray();

        if (fragments.Length == 0)
        {
            var error = new OpenApiDocumentParsingError(
                "Document must contain either a single operation or at least one fragment definition.",
                document);
            return OpenApiDocumentParsingResult.Failure(error);
        }

        var fragmentDefinition = fragments[0];

        return ParseFragment(fragmentDefinition, document);
    }

    private static OpenApiDocumentParsingResult ParseFragment(
        FragmentDefinitionNode fragment,
        DocumentNode document)
    {
        var name = fragment.Name.Value;
        var description = fragment.Description?.Value;

        var fragmentReferences = FragmentReferenceFinder.Find(document, fragment);

        var fragmentDocument = new OpenApiFragmentDocument(
            name,
            description,
            fragment,
            fragmentReferences.Local,
            fragmentReferences.External);

        return OpenApiDocumentParsingResult.Success(fragmentDocument);
    }

    private static OpenApiDocumentParsingResult ParseOperation(
        OperationDefinitionNode operation,
        DocumentNode document)
    {
        var description = operation.Description?.Value;

        var httpDirective = operation.Directives.FirstOrDefault(d => d.Name.Value == WellKnownDirectiveNames.Http);

        if (httpDirective is null)
        {
            var error = new OpenApiDocumentParsingError(
                $"Operation must be annotated with @http directive.",
                document);
            return OpenApiDocumentParsingResult.Failure(error);
        }

        if (!TryParseHttpMethod(httpDirective, document, out var httpMethod, out var httpMethodError))
        {
            return OpenApiDocumentParsingResult.Failure(httpMethodError);
        }

        if (!TryParseRoute(httpDirective, document, out var route, out var routeError))
        {
            return OpenApiDocumentParsingResult.Failure(routeError);
        }

        if (!TryParseQueryParameters(httpDirective, document, out var queryParameters, out var queryParametersError))
        {
            return OpenApiDocumentParsingResult.Failure(queryParametersError);
        }

        var bodyParameter = GetBodyParameter(operation);

        var cleanOperation = RewriteOperation(operation);
        cleanOperation = cleanOperation
            .WithVariableDefinitions(
                cleanOperation.VariableDefinitions
                    .Select(RewriteVariableDefinition)
                    .ToArray());

        var fragmentReferences = FragmentReferenceFinder.Find(document, operation);

        var operationDocument = new OpenApiOperationDocument(
            description,
            httpMethod,
            route,
            queryParameters,
            bodyParameter,
            cleanOperation,
            fragmentReferences.Local,
            fragmentReferences.External);

        return OpenApiDocumentParsingResult.Success(operationDocument);
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
        DocumentNode document,
        out ImmutableArray<OpenApiRouteSegmentParameter> queryParameters,
        [NotNullWhen(false)] out OpenApiDocumentParsingError? error)
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
            if (!TryParseParameter(singleValue, document, out var parameter, out error))
            {
                return false;
            }

            queryParameters = [parameter];
            error = null;
            return true;
        }

        if (value is not ListValueNode listValue)
        {
            error = new OpenApiDocumentParsingError(
                "Query parameters argument on @http directive must be a list of strings.",
                document);
            return false;
        }

        var builder = ImmutableArray.CreateBuilder<OpenApiRouteSegmentParameter>(listValue.Items.Count);

        foreach (var item in listValue.Items)
        {
            if (item is not StringValueNode { Value: var stringValue })
            {
                error = new OpenApiDocumentParsingError(
                    "Query parameters argument on @http directive must contain only string values.",
                    document);
                return false;
            }

            if (!TryParseParameter(stringValue, document, out var parameter, out error))
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
        DocumentNode document,
        [NotNullWhen(true)] out OpenApiRoute? route,
        [NotNullWhen(false)] out OpenApiDocumentParsingError? error)
    {
        route = null;

        var routeArgument = httpDirective.Arguments
            .FirstOrDefault(x => x.Name.Value == WellKnownArgumentNames.Route);

        if (routeArgument is null)
        {
            error = new OpenApiDocumentParsingError(
                "@http directive must have a 'route' argument.",
                document);
            return false;
        }

        var value = routeArgument.Value;

        if (value is not StringValueNode { Value: var stringValue } || string.IsNullOrEmpty(stringValue))
        {
            error = new OpenApiDocumentParsingError(
                "Route argument on @http directive must be a non-empty string.",
                document);
            return false;
        }

        var segments = stringValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var builder = ImmutableArray.CreateBuilder<IOpenApiRouteSegment>(segments.Length);

        foreach (var segment in segments)
        {
            if (segment.StartsWith("{") && segment.EndsWith("}"))
            {
                var parameter = segment[1..^1];
                if (!TryParseParameter(parameter, document, out var parsedParameter, out error))
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
        DocumentNode document,
        [NotNullWhen(true)] out OpenApiRouteSegmentParameter? parsedParameter,
        [NotNullWhen(false)] out OpenApiDocumentParsingError? error)
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
            error = new OpenApiDocumentParsingError(
                $"Explicit route segment variable mappings must start with '$', got '{parameter}'.",
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
                ? []
                : pathSpan[(dotIndex + 1)..];
        }

        parsedParameter = new OpenApiRouteSegmentParameter(key, variable, builder.MoveToImmutable());
        error = null;
        return true;
    }

    private static bool TryParseHttpMethod(
        DirectiveNode httpDirective,
        DocumentNode document,
        [NotNullWhen(true)] out string? httpMethod,
        [NotNullWhen(false)] out OpenApiDocumentParsingError? error)
    {
        httpMethod = null;

        var methodArgument = httpDirective.Arguments
            .FirstOrDefault(x => x.Name.Value == WellKnownArgumentNames.Method);

        if (methodArgument is null)
        {
            error = new OpenApiDocumentParsingError(
                $"@http directive must have a 'method' argument.",
                document);
            return false;
        }

        var value = methodArgument.Value;

        if (value is not EnumValueNode { Value: var stringValue } || string.IsNullOrEmpty(stringValue))
        {
            error = new OpenApiDocumentParsingError(
                $"Method argument on @http directive must be a non-empty enum value.",
                document);
            return false;
        }

        if (stringValue != HttpMethods.Get
            && stringValue != HttpMethods.Post
            && stringValue != HttpMethods.Put
            && stringValue != HttpMethods.Patch
            && stringValue != HttpMethods.Delete)
        {
            error = new OpenApiDocumentParsingError(
                $"Invalid HTTP method value '{stringValue}' on @http directive.",
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
}
