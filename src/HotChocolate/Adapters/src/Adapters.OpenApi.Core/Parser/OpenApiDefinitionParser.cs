using System.Collections.Immutable;
using System.Text;
using HotChocolate.Adapters.OpenApi.Validation;
using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Parses OpenAPI definitions from GraphQL documents.
/// </summary>
public static class OpenApiDefinitionParser
{
    /// <summary>
    /// Parses an OpenAPI definition from a GraphQL document.
    /// </summary>
    /// <param name="document">The GraphQL document to parse.</param>
    /// <returns>The parsed OpenAPI definition.</returns>
    /// <exception cref="OpenApiDefinitionParsingException">
    /// Thrown when the document cannot be parsed as a valid OpenAPI definition.
    /// </exception>
    public static IOpenApiDefinition Parse(DocumentNode document)
    {
        // An operation document can only define a single operation alongside local fragment definitions.
        var operationDefinitions = document.Definitions.OfType<OperationDefinitionNode>().ToArray();

        if (operationDefinitions.Length > 1)
        {
            throw new OpenApiDefinitionParsingException(
                "Document must contain either a single operation or at least one fragment definition.");
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
            throw new OpenApiDefinitionParsingException(
                "Document must contain either a single operation or at least one fragment definition.");
        }

        var fragmentDefinition = fragments[0];

        return ParseFragment(fragmentDefinition, document);
    }

    private static OpenApiModelDefinition ParseFragment(
        FragmentDefinitionNode fragment,
        DocumentNode document)
    {
        var name = fragment.Name.Value;
        var description = fragment.Description?.Value;

        var fragmentReferences = FragmentReferenceFinder.Find(document, fragment);

        return new OpenApiModelDefinition(
            name,
            description,
            document,
            fragment,
            fragmentReferences.Local,
            fragmentReferences.External);
    }

    private static OpenApiEndpointDefinition ParseOperation(
        OperationDefinitionNode operation,
        DocumentNode document)
    {
        var description = operation.Description?.Value;

        var httpDirective = operation.Directives.FirstOrDefault(d => d.Name.Value == WellKnownDirectiveNames.Http);

        if (httpDirective is null)
        {
            throw new OpenApiDefinitionParsingException(
                "Operation must be annotated with @http directive.");
        }

        var httpMethod = ParseHttpMethod(httpDirective);
        var (route, routeParameters) = ParseRoute(httpDirective);
        var queryParameters = ParseQueryParameters(httpDirective);

        var bodyVariableName = GetBodyVariableName(operation);

        var cleanOperation = RewriteOperation(operation);

        List<IDefinitionNode> cleanDefinitions = [cleanOperation, .. document.Definitions];
        cleanDefinitions.Remove(operation);

        var fragmentReferences = FragmentReferenceFinder.Find(document);

        return new OpenApiEndpointDefinition(
            httpMethod,
            route,
            description,
            [.. routeParameters],
            [.. queryParameters],
            bodyVariableName,
            new DocumentNode(cleanDefinitions),
            cleanOperation,
            fragmentReferences.Local,
            fragmentReferences.External);
    }

    private static string? GetBodyVariableName(OperationDefinitionNode operation)
    {
        var variableWithBodyDirective = operation.VariableDefinitions
            .FirstOrDefault(v => v.Directives
                .Any(d => d.Name.Value == WellKnownDirectiveNames.Body));

        return variableWithBodyDirective?.Variable.Name.Value;
    }

    private static List<OpenApiEndpointDefinitionParameter> ParseQueryParameters(DirectiveNode httpDirective)
    {
        var value = httpDirective.Arguments
            .FirstOrDefault(x => x.Name.Value == WellKnownArgumentNames.QueryParameters)?.Value;

        if (value is null)
        {
            return [];
        }

        if (value is StringValueNode { Value: var singleValue })
        {
            var parameter = ParseParameter(singleValue);
            return [parameter];
        }

        if (value is not ListValueNode listValue)
        {
            throw new OpenApiDefinitionParsingException(
                $"'{WellKnownArgumentNames.QueryParameters}' argument on @http directive must be a list of strings.");
        }

        var parameters = new List<OpenApiEndpointDefinitionParameter>();

        foreach (var item in listValue.Items)
        {
            if (item is not StringValueNode { Value: var stringValue })
            {
                throw new OpenApiDefinitionParsingException(
                    $"'{WellKnownArgumentNames.QueryParameters}' argument on @http directive must contain only string values.");
            }

            var parameter = ParseParameter(stringValue);
            parameters.Add(parameter);
        }

        return parameters;
    }

    private static (string Route, List<OpenApiEndpointDefinitionParameter> Parameters) ParseRoute(
        DirectiveNode httpDirective)
    {
        var routeArgument = httpDirective.Arguments
            .FirstOrDefault(x => x.Name.Value == WellKnownArgumentNames.Route);

        if (routeArgument is null)
        {
            throw new OpenApiDefinitionParsingException(
                $"@http directive must have a '{WellKnownArgumentNames.Route}' argument.");
        }

        var value = routeArgument.Value;

        if (value is not StringValueNode { Value: var stringValue } || string.IsNullOrEmpty(stringValue))
        {
            throw new OpenApiDefinitionParsingException(
                $"'{WellKnownArgumentNames.Route}' argument on @http directive must be a non-empty string.");
        }

        var routeBuilder = new StringBuilder();
        var parameters = new List<OpenApiEndpointDefinitionParameter>();

        foreach (var segment in stringValue.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            routeBuilder.Append('/');

            if (segment.StartsWith("{") && segment.EndsWith("}"))
            {
                var parameterKey = segment[1..^1];
                var parameter = ParseParameter(parameterKey);

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

        return (routeBuilder.ToString(), parameters);
    }

    private static OpenApiEndpointDefinitionParameter ParseParameter(string input)
    {
        var span = input.AsSpan();
        var colonIndex = span.IndexOf(':');

        if (colonIndex == -1)
        {
            return new OpenApiEndpointDefinitionParameter(input, input, null);
        }

        if (colonIndex + 1 >= span.Length || span[colonIndex + 1] != '$')
        {
            throw new OpenApiDefinitionParsingException(
                $"Parameter variable mappings must start with '$', got '{input}'.");
        }

        var key = span[..colonIndex].ToString();

        // Skip ':$'
        var mappingSyntax = span[(colonIndex + 2)..];

        var firstDotIndex = mappingSyntax.IndexOf('.');

        if (firstDotIndex == -1)
        {
            return new OpenApiEndpointDefinitionParameter(
                key,
                mappingSyntax.ToString(),
                null);
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

        return new OpenApiEndpointDefinitionParameter(
            key,
            variable,
            builder.MoveToImmutable());
    }

    private static string ParseHttpMethod(DirectiveNode httpDirective)
    {
        var methodArgument = httpDirective.Arguments
            .FirstOrDefault(x => x.Name.Value == WellKnownArgumentNames.Method);

        if (methodArgument is null)
        {
            throw new OpenApiDefinitionParsingException(
                $"@http directive must have a '{WellKnownArgumentNames.Method}' argument.");
        }

        var value = methodArgument.Value;

        if (value is not EnumValueNode { Value: var stringValue } || string.IsNullOrEmpty(stringValue))
        {
            throw new OpenApiDefinitionParsingException(
                $"'{WellKnownArgumentNames.Method}' argument on @http directive must be a non-empty enum value.");
        }

        if (!EndpointHttpMethodMustBeValidRule.IsValidHttpMethod(stringValue))
        {
            throw new OpenApiDefinitionParsingException(
                $"'{WellKnownArgumentNames.Method}' argument on @http directive received an invalid value '{stringValue}'.");
        }

        return stringValue;
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
