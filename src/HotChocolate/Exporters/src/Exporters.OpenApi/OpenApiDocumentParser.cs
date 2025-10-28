using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Exporters.OpenApi;

public sealed class OpenApiDocumentParser(ISchemaDefinition schema)
{
    private static readonly FragmentSpreadFinder s_fragmentSpreadFinder = new();

    public IOpenApiDocument Parse(string id, DocumentNode document)
    {
        var fragmentDefinitions = document.Definitions.OfType<FragmentDefinitionNode>().ToArray();

        // An operation document can only define a single operation alongside local fragment definitions.
        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().SingleOrDefault();

        if (operationDefinition is not null)
        {
            return ParseOperation(id, operationDefinition, fragmentDefinitions);
        }

        // A fragment document can only define a single fragment.
        var fragmentDefinition = fragmentDefinitions.Single();

        return ParseFragment(id, fragmentDefinition);
    }

    private OpenApiFragmentDocument ParseFragment(string id, FragmentDefinitionNode fragment)
    {
        var name = fragment.Name.Value;
        var description = fragment.Description?.Value;

        if (!schema.Types.TryGetType(fragment.TypeCondition.Name.Value, out var typeCondition))
        {
            throw new InvalidOperationException("Type Condition not found");
        }

        var context = new FragmentSpreadFinderContext();
        s_fragmentSpreadFinder.Visit(fragment, context);

        return new OpenApiFragmentDocument(
            id,
            name,
            description,
            typeCondition,
            fragment,
            context.FragmentDependencies);
    }

    private static OpenApiOperationDocument ParseOperation(
        string id,
        OperationDefinitionNode operation,
        FragmentDefinitionNode[] localFragments)
    {
        var name = operation.Name?.Value;
        var description = operation.Description?.Value;

        if (string.IsNullOrEmpty(name))
        {
            throw new InvalidOperationException("Operation name not found");
        }

        var httpDirective = operation.Directives.First(d => d.Name.Value == WellKnownDirectiveNames.Http);

        var httpMethod = ParseHttpMethod(httpDirective);
        var route = ParseRoute(httpDirective);
        var queryParameters = ParseQueryParameters(httpDirective);
        var bodyParameter = GetBodyParameter(operation);

        var cleanOperation = RewriteOperation(operation);
        cleanOperation = cleanOperation
            .WithVariableDefinitions(
                cleanOperation.VariableDefinitions
                    .Select(RewriteVariableDefinition)
                    .ToArray());

        var context = new FragmentSpreadFinderContext();
        s_fragmentSpreadFinder.Visit(operation, context);

        return new OpenApiOperationDocument(
            id,
            name,
            description,
            httpMethod,
            route,
            queryParameters,
            bodyParameter,
            cleanOperation,
            context.FragmentDependencies);
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

    private static ImmutableArray<OpenApiRouteSegmentParameter> ParseQueryParameters(DirectiveNode httpDirective)
    {
        var value = httpDirective.Arguments
            .FirstOrDefault(x => x.Name.Value == WellKnownArgumentNames.QueryParameters)?.Value;

        if (value is null)
        {
            return [];
        }

        if (value is StringValueNode { Value: var singleValue })
        {
            return [ParseParameter(singleValue)];
        }

        if (value is not ListValueNode listValue)
        {
            throw new InvalidOperationException("Query parameters must be a list of strings");
        }

        var builder = ImmutableArray.CreateBuilder<OpenApiRouteSegmentParameter>(listValue.Items.Count);

        foreach (var item in listValue.Items)
        {
            if (item.Value is not StringValueNode { Value: var stringValue })
            {
                throw new InvalidOperationException("Query parameters must be a string");
            }

            builder.Add(ParseParameter(stringValue));
        }

        return builder.MoveToImmutable();
    }

    private static OpenApiRoute ParseRoute(DirectiveNode httpDirective)
    {
        var value = httpDirective.Arguments
            .First(x => x.Name.Value == WellKnownArgumentNames.Route).Value;

        if (value is not StringValueNode { Value: var stringValue } || string.IsNullOrEmpty(stringValue))
        {
            throw new InvalidOperationException("Expected http route to be a non-empty string");
        }

        var segments = stringValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var builder = ImmutableArray.CreateBuilder<IOpenApiRouteSegment>(segments.Length);

        foreach (var segment in segments)
        {
            if (segment.StartsWith("{") && segment.EndsWith("}"))
            {
                var parameter = segment.Substring(1, segment.Length - 2);

                builder.Add(ParseParameter(parameter));
            }
            else
            {
                builder.Add(new OpenApiRouteSegmentLiteral(segment));
            }
        }

        return new OpenApiRoute(builder.MoveToImmutable());
    }

    private static OpenApiRouteSegmentParameter ParseParameter(string parameter)
    {
        var span = parameter.AsSpan();
        var colonIndex = span.IndexOf(':');

        if (colonIndex == -1)
        {
            return new OpenApiRouteSegmentParameter(parameter, parameter, null);
        }

        if (colonIndex + 1 >= span.Length || span[colonIndex + 1] != '$')
        {
            throw new InvalidOperationException(
                $"Explicit route segment variable mappings must start with '$', got '{parameter}'");
        }

        var key = span[..colonIndex].ToString();

        // Skip ':$'
        var mappingSyntax = span[(colonIndex + 2)..];

        var firstDotIndex = mappingSyntax.IndexOf('.');

        if (firstDotIndex == -1)
        {
            return new OpenApiRouteSegmentParameter(key, mappingSyntax.ToString(), null);
        }

        var variable = mappingSyntax[..firstDotIndex].ToString();
        var pathSpan = mappingSyntax[(firstDotIndex + 1)..];

        var segmentCount = 1;
        for (var i = 0; i < pathSpan.Length; i++)
        {
            if (i == '.')
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

        return new OpenApiRouteSegmentParameter(key, variable, builder.MoveToImmutable());

        // var colonIndex = parameter.IndexOf(':');
        //
        // if (colonIndex == -1)
        // {
        //     return new OpenApiRouteSegmentParameter(parameter, parameter, null);
        // }
        //
        // if (parameter[colonIndex + 1] != '$')
        // {
        //     throw new InvalidOperationException(
        //         $"Explicit route segment variable mappings must start with '$', got '{parameter}'");
        // }
        //
        // var mappingSyntax = parameter[(colonIndex + 1)..];
        //
        // var mapSegments = mappingSyntax.Split('.');
        //
        // var key = parameter[..colonIndex];
        // var variable = mapSegments[0][1..];
        //
        // return mapSegments.Length != 1
        //     ? new OpenApiRouteSegmentParameter(key, variable, mapSegments[1..])
        //     : new OpenApiRouteSegmentParameter(key, variable, null);
    }

    private static string ParseHttpMethod(DirectiveNode httpDirective)
    {
        var value = httpDirective.Arguments
            .First(x => x.Name.Value == WellKnownArgumentNames.Method).Value;

        if (value is not EnumValueNode { Value: var stringValue } || string.IsNullOrEmpty(stringValue))
        {
            throw new InvalidOperationException("Expected http method to be a non-empty string");
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
            throw new InvalidOperationException("Invalid http method value: " + stringValue);
        }

        return stringValue;
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

    private sealed class FragmentSpreadFinder : SyntaxVisitor<FragmentSpreadFinderContext>
    {
        protected override ISyntaxVisitorAction VisitChildren(
            FragmentSpreadNode node,
            FragmentSpreadFinderContext context)
        {
            context.FragmentDependencies.Add(node.Name.Value);

            return Continue;
        }
    }

    private sealed class FragmentSpreadFinderContext
    {
        public List<string> FragmentDependencies { get; } = [];
    }
}
