using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.Adapters.OpenApi;

internal static class OpenApiEndpointFactory
{
    public static Endpoint Create(
        OpenApiOperationDocument operationDocument,
        Dictionary<string, OpenApiFragmentDocument> fragmentsByName,
        ISchemaDefinition schema)
    {
        var endpointDescriptor = CreateEndpointDescriptor(operationDocument, fragmentsByName, schema);

        return CreateEndpoint(schema.Name, endpointDescriptor);
    }

    private static Endpoint CreateEndpoint(string schemaName, OpenApiEndpointDescriptor endpointDescriptor)
    {
        var requestDelegate = CreateRequestDelegate(schemaName, endpointDescriptor);

        var builder = new RouteEndpointBuilder(
            requestDelegate: requestDelegate,
            routePattern: endpointDescriptor.Route,
            // TODO: What does this control?
            order: 0)
        {
            DisplayName = endpointDescriptor.Route.RawText
        };

        builder.Metadata.Add(new HttpMethodMetadata([endpointDescriptor.HttpMethod]));

        return builder.Build();
    }

    private static RequestDelegate CreateRequestDelegate(
        string schemaName,
        OpenApiEndpointDescriptor endpointDescriptor)
    {
        var middleware = new DynamicEndpointMiddleware(schemaName, endpointDescriptor);
        return context => middleware.InvokeAsync(context);
    }

    private static OpenApiEndpointDescriptor CreateEndpointDescriptor(
        OpenApiOperationDocument operationDocument,
        Dictionary<string, OpenApiFragmentDocument> fragmentsByName,
        ISchemaDefinition schema)
    {
        var definitions = new List<IExecutableDefinitionNode>();
        definitions.Add(operationDocument.OperationDefinition);

        foreach (var referencedFragmentName in operationDocument.ExternalFragmentReferences)
        {
            var fragmentDocument = fragmentsByName[referencedFragmentName];

            definitions.Add(fragmentDocument.FragmentDefinition);
        }

        var document = new DocumentNode(definitions);

        var rootField = operationDocument.OperationDefinition.SelectionSet.Selections
            .OfType<FieldNode>()
            .First();

        var responseNameToExtract = rootField.Alias?.Value ?? rootField.Name.Value;

        var route = CreateRoutePattern(operationDocument.Route);

        var parameterTrie = new VariableValueInsertionTrie();

        InsertParametersIntoTrie(operationDocument.Route.Parameters, OpenApiEndpointParameterType.Route);
        InsertParametersIntoTrie(operationDocument.QueryParameters, OpenApiEndpointParameterType.Query);

        return new OpenApiEndpointDescriptor(
            document,
            operationDocument.HttpMethod,
            route,
            parameterTrie,
            operationDocument.BodyParameter?.VariableName,
            responseNameToExtract);

        void InsertParametersIntoTrie(
            IEnumerable<OpenApiRouteSegmentParameter> parameters,
            OpenApiEndpointParameterType parameterType)
        {
            foreach (var parameter in parameters)
            {
                var inputType = GetTypeFromParameter(parameter, operationDocument.OperationDefinition, schema);

                var leaf = new VariableValueInsertionTrieLeaf(
                    parameter.Key,
                    inputType,
                    parameterType);

                var inputObjectPath = parameter.InputObjectPath;

                if (!inputObjectPath.HasValue)
                {
                    parameterTrie[parameter.VariableName] = leaf;
                }
                else
                {
                    VariableValueInsertionTrie variableTrie;
                    if (!parameterTrie.TryGetValue(parameter.VariableName, out var existingVariableSegment))
                    {
                        variableTrie = new VariableValueInsertionTrie();
                        parameterTrie[parameter.VariableName] = variableTrie;
                    }
                    else if (existingVariableSegment is VariableValueInsertionTrie existingVariableTrie)
                    {
                        variableTrie = existingVariableTrie;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    var currentTrie = variableTrie;
                    var path = inputObjectPath.Value;

                    for (var i = 0; i < path.Length - 1; i++)
                    {
                        var fieldName = path[i];
                        if (!currentTrie.TryGetValue(fieldName, out var existingSegment))
                        {
                            var newTrie = new VariableValueInsertionTrie();
                            currentTrie[fieldName] = newTrie;
                            currentTrie = newTrie;
                        }
                        else if (existingSegment is VariableValueInsertionTrie existingTrie)
                        {
                            currentTrie = existingTrie;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    currentTrie[path[^1]] = leaf;
                }
            }
        }
    }

    private static ITypeDefinition GetTypeFromParameter(
        OpenApiRouteSegmentParameter parameter,
        OperationDefinitionNode operation,
        ISchemaDefinition schema)
    {
        var variable = operation.VariableDefinitions
            .First(v => v.Variable.Name.Value == parameter.VariableName);

        var currentType = schema.Types[variable.Type.NamedType().Name.Value];

        if (parameter.InputObjectPath is { Length: > 0 })
        {
            foreach (var fieldName in parameter.InputObjectPath)
            {
                if (currentType is not IInputObjectTypeDefinition inputObjectType
                    || !inputObjectType.Fields.TryGetField(fieldName, out var field))
                {
                    throw new InvalidOperationException(
                        $"Expected type '{currentType.Name}' to have a field named '{fieldName}'.");
                }

                currentType = field.Type.NamedType();
            }
        }

        return currentType;
    }

    private static RoutePattern CreateRoutePattern(OpenApiRoute route)
    {
        var segments = new List<RoutePatternPathSegment>();

        foreach (var segment in route.Segments)
        {
            if (segment is OpenApiRouteSegmentLiteral stringSegment)
            {
                segments.Add(
                    RoutePatternFactory.Segment(
                        RoutePatternFactory.LiteralPart(stringSegment.Value)));
            }
            else if (segment is OpenApiRouteSegmentParameter mapSegment)
            {
                // We do not apply route constraints here, as they are not meant for validation but to disambiguate routes:
                // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing#route-constraints
                segments.Add(
                    RoutePatternFactory.Segment(
                        RoutePatternFactory.ParameterPart(mapSegment.Key)));
            }
        }

        return RoutePatternFactory.Pattern(segments);
    }
}
