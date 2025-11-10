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

        var numberOfParameters = operationDocument.Route.Parameters.Length + operationDocument.QueryParameters.Length;
        var parameters = ImmutableArray.CreateBuilder<OpenApiEndpointParameterDescriptor>(numberOfParameters);
        var hasRouteParameters = false;

        foreach (var routeParameter in operationDocument.Route.Parameters)
        {
            var inputType = GetTypeFromParameter(routeParameter, operationDocument.OperationDefinition, schema);
            var parameterDescriptor = new OpenApiEndpointParameterDescriptor(
                routeParameter.Key,
                routeParameter.Variable,
                routeParameter.InputObjectPath,
                inputType,
                OpenApiEndpointParameterType.Route);

            parameters.Add(parameterDescriptor);

            if (!hasRouteParameters)
            {
                hasRouteParameters = true;
            }
        }

        foreach (var queryParameter in operationDocument.QueryParameters)
        {
            var inputType = GetTypeFromParameter(queryParameter, operationDocument.OperationDefinition, schema);
            var parameterDescriptor = new OpenApiEndpointParameterDescriptor(
                queryParameter.Key,
                queryParameter.Variable,
                queryParameter.InputObjectPath,
                inputType,
                OpenApiEndpointParameterType.Query);

            parameters.Add(parameterDescriptor);
        }

        return new OpenApiEndpointDescriptor(
            document,
            operationDocument.HttpMethod,
            route,
            parameters.MoveToImmutable(),
            hasRouteParameters,
            operationDocument.BodyParameter?.Variable,
            responseNameToExtract);
    }

    private static ITypeDefinition GetTypeFromParameter(
        OpenApiRouteSegmentParameter parameter,
        OperationDefinitionNode operation,
        ISchemaDefinition schema)
    {
        var variable = operation.VariableDefinitions
            .First(v => v.Variable.Name.Value == parameter.Variable);

        var variableType = schema.Types[variable.Type.NamedType().Name.Value];

        // TODO: Visit input object path

        return variableType;
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
