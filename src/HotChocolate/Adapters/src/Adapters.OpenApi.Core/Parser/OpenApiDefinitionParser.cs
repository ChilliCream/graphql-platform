using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Adapters.OpenApi;

public static class OpenApiDefinitionParser
{
    public static OpenApiDefinitionParsingResult Parse(DocumentNode document)
    {
        // An operation document can only define a single operation alongside local fragment definitions.
        var operationDefinitions = document.Definitions.OfType<OperationDefinitionNode>().ToArray();

        if (operationDefinitions.Length > 1)
        {
            var error = new OpenApiDefinitionParsingError(
                "Document must contain either a single operation or at least one fragment definition.",
                document);
            return OpenApiDefinitionParsingResult.Failure(error);
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
            var error = new OpenApiDefinitionParsingError(
                "Document must contain either a single operation or at least one fragment definition.",
                document);
            return OpenApiDefinitionParsingResult.Failure(error);
        }

        var fragmentDefinition = fragments[0];

        return ParseFragment(fragmentDefinition, document);
    }

    private static OpenApiDefinitionParsingResult ParseFragment(
        FragmentDefinitionNode fragment,
        DocumentNode document)
    {
        var name = fragment.Name.Value;
        var description = fragment.Description?.Value;

        var fragmentReferences = FragmentReferenceFinder.Find(document, fragment);

        var modelDefinition = new OpenApiModelDefinition(
            name,
            description,
            document,
            fragmentReferences.Local,
            fragmentReferences.External);

        return OpenApiDefinitionParsingResult.Success(modelDefinition);
    }

    private static OpenApiDefinitionParsingResult ParseOperation(
        OperationDefinitionNode operation,
        DocumentNode document)
    {
        var description = operation.Description?.Value;

        var httpDirective = operation.Directives.FirstOrDefault(d => d.Name.Value == WellKnownDirectiveNames.Http);

        if (httpDirective is null)
        {
            var error = new OpenApiDefinitionParsingError(
                $"Operation must be annotated with @http directive.",
                document);
            return OpenApiDefinitionParsingResult.Failure(error);
        }

        if (!TryParseHttpMethod(httpDirective, document, out var httpMethod, out var httpMethodError))
        {
            return OpenApiDefinitionParsingResult.Failure(httpMethodError);
        }

        if (!TryParseRoute(httpDirective, document, out var route, out var routeParameters, out var routeError))
        {
            return OpenApiDefinitionParsingResult.Failure(routeError);
        }

        if (!TryParseQueryParameters(httpDirective, document, out var queryParameters, out var queryParametersError))
        {
            return OpenApiDefinitionParsingResult.Failure(queryParametersError);
        }

        var bodyVariableName = GetBodyVariableName(operation);

        var cleanOperation = RewriteOperation(operation);

        List<IDefinitionNode> cleanDefinitions = [cleanOperation, ..document.Definitions];
        cleanDefinitions.Remove(operation);

        var fragmentReferences = FragmentReferenceFinder.Find(document);

        var endpointDefinition = new OpenApiEndpointDefinition(
            httpMethod,
            route,
            description,
            [..routeParameters],
            [..queryParameters],
            bodyVariableName,
            new DocumentNode(cleanDefinitions),
            fragmentReferences.Local,
            fragmentReferences.External);

        return OpenApiDefinitionParsingResult.Success(endpointDefinition);
    }

    private static string? GetBodyVariableName(OperationDefinitionNode operation)
    {
        var variableWithBodyDirective = operation.VariableDefinitions
            .FirstOrDefault(v => v.Directives
                .Any(d => d.Name.Value == WellKnownDirectiveNames.Body));

        return variableWithBodyDirective?.Variable.Name.Value;
    }

    private static bool TryParseQueryParameters(
        DirectiveNode httpDirective,
        DocumentNode document,
        out List<OpenApiEndpointDefinitionParameter> parameters,
        [NotNullWhen(false)] out OpenApiDefinitionParsingError? error)
    {
        parameters = [];

        var value = httpDirective.Arguments
            .FirstOrDefault(x => x.Name.Value == WellKnownArgumentNames.QueryParameters)?.Value;

        if (value is null)
        {
            parameters = [];
            error = null;
            return true;
        }

        if (value is StringValueNode { Value: var singleValue })
        {
            if (!TryParseParameter(singleValue, document, out var parameter, out error))
            {
                return false;
            }

            parameters = [parameter];
            error = null;
            return true;
        }

        if (value is not ListValueNode listValue)
        {
            error = new OpenApiDefinitionParsingError(
                $"'{WellKnownArgumentNames.QueryParameters}' argument on @http directive must be a list of strings.",
                document);
            return false;
        }

        parameters = [];

        foreach (var item in listValue.Items)
        {
            if (item is not StringValueNode { Value: var stringValue })
            {
                error = new OpenApiDefinitionParsingError(
                    $"'{WellKnownArgumentNames.QueryParameters}' argument on @http directive must contain only string values.",
                    document);
                return false;
            }

            if (!TryParseParameter(stringValue, document, out var parameter, out error))
            {
                return false;
            }

            parameters.Add(parameter);
        }

        error = null;
        return true;
    }

    private static bool TryParseRoute(
        DirectiveNode httpDirective,
        DocumentNode document,
        [NotNullWhen(true)] out string? route,
        [NotNullWhen(true)] out List<OpenApiEndpointDefinitionParameter>? parameters,
        [NotNullWhen(false)] out OpenApiDefinitionParsingError? error)
    {
        route = null;
        parameters = null;

        var routeArgument = httpDirective.Arguments
            .FirstOrDefault(x => x.Name.Value == WellKnownArgumentNames.Route);

        if (routeArgument is null)
        {
            error = new OpenApiDefinitionParsingError(
                $"@http directive must have a '{WellKnownArgumentNames.Route}' argument.",
                document);
            return false;
        }

        var value = routeArgument.Value;

        if (value is not StringValueNode { Value: var stringValue } || string.IsNullOrEmpty(stringValue))
        {
            error = new OpenApiDefinitionParsingError(
                $"'{WellKnownArgumentNames.Route}' argument on @http directive must be a non-empty string.",
                document);
            return false;
        }

        var routeBuilder = new StringBuilder();
        parameters = [];

        foreach (var segment in stringValue.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            routeBuilder.Append("/");

            if (segment.StartsWith("{") && segment.EndsWith("}"))
            {
                var parameterKey = segment[1..^1];
                if (!TryParseParameter(parameterKey, document, out var parameter, out error))
                {
                    return false;
                }

                parameters.Add(parameter);

                routeBuilder.Append('{');
                routeBuilder.Append(parameter.Key);
                routeBuilder.Append('}');
            }
            else
            {
                routeBuilder.Append(segment);
            }
        }

        route = routeBuilder.ToString();
        error = null;

        return true;
    }

    private static bool TryParseParameter(
        string input,
        DocumentNode document,
        [NotNullWhen(true)] out OpenApiEndpointDefinitionParameter? parameter,
        [NotNullWhen(false)] out OpenApiDefinitionParsingError? error)
    {
        parameter = null;

        var span = input.AsSpan();
        var colonIndex = span.IndexOf(':');

        if (colonIndex == -1)
        {
            parameter = new OpenApiEndpointDefinitionParameter(input, input, null);
            error = null;
            return true;
        }

        if (colonIndex + 1 >= span.Length || span[colonIndex + 1] != '$')
        {
            error = new OpenApiDefinitionParsingError(
                $"Parameter variable mappings must start with '$', got '{input}'.",
                document);
            return false;
        }

        var key = span[..colonIndex].ToString();

        // Skip ':$'
        var mappingSyntax = span[(colonIndex + 2)..];

        var firstDotIndex = mappingSyntax.IndexOf('.');

        if (firstDotIndex == -1)
        {
            parameter = new OpenApiEndpointDefinitionParameter(
                key,
                mappingSyntax.ToString(),
                null);
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

        parameter = new OpenApiEndpointDefinitionParameter(
            key,
            variable,
            builder.MoveToImmutable());
        error = null;
        return true;
    }

    private static bool TryParseHttpMethod(
        DirectiveNode httpDirective,
        DocumentNode document,
        [NotNullWhen(true)] out string? httpMethod,
        [NotNullWhen(false)] out OpenApiDefinitionParsingError? error)
    {
        httpMethod = null;

        var methodArgument = httpDirective.Arguments
            .FirstOrDefault(x => x.Name.Value == WellKnownArgumentNames.Method);

        if (methodArgument is null)
        {
            error = new OpenApiDefinitionParsingError(
                $"@http directive must have a '{WellKnownArgumentNames.Method}' argument.",
                document);
            return false;
        }

        var value = methodArgument.Value;

        if (value is not EnumValueNode { Value: var stringValue } || string.IsNullOrEmpty(stringValue))
        {
            error = new OpenApiDefinitionParsingError(
                $"'{WellKnownArgumentNames.Method}' argument on @http directive must be a non-empty enum value.",
                document);
            return false;
        }

        if (stringValue != HttpMethods.Get
            && stringValue != HttpMethods.Post
            && stringValue != HttpMethods.Put
            && stringValue != HttpMethods.Patch
            && stringValue != HttpMethods.Delete)
        {
            error = new OpenApiDefinitionParsingError(
                $"'{WellKnownArgumentNames.Method}' argument on @http directive received an invalid value '{stringValue}'.",
                document);
            return false;
        }

        httpMethod = stringValue;
        error = null;
        return true;
    }

    private static OperationDefinitionNode RewriteOperation(OperationDefinitionNode operation)
    {
        var directives = operation.Directives
            .Where(v => v.Name.Value != WellKnownDirectiveNames.Http)
            .ToArray();

        return operation
            .WithDescription(null)
            .WithDirectives(directives)
            .WithVariableDefinitions(
            operation.VariableDefinitions
                    .Select(RewriteVariableDefinition)
                    .ToArray());
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
