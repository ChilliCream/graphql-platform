using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Exporters.OpenApi.Validation;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.Exporters.OpenApi;

// TODO: Make this nicer and independent from executor lifetime
internal sealed class OpenApiWarmupTask(
    IOpenApiDefinitionStorage definitionStorage,
    DynamicOpenApiDocumentTransformer transformer,
    IDynamicEndpointDataSource dynamicEndpointDataSource) : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => true;

    public async Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken)
    {
        var schema = executor.Schema;
        var documents = await definitionStorage.GetDocumentsAsync(cancellationToken);

        // TODO: Maybe this can be static without the schema reference?
        var parser = new OpenApiDocumentParser(schema);
        var fragmentDocumentLookup = new Dictionary<string, OpenApiFragmentDocument>();
        var operationDocuments = new List<OpenApiOperationDocument>();

        foreach (var document in documents)
        {
            var parsedDocument = parser.Parse(document.Id, document.Document);

            if (parsedDocument is OpenApiFragmentDocument fragmentDocument)
            {
                fragmentDocumentLookup.Add(fragmentDocument.Name, fragmentDocument);
            }
            else if (parsedDocument is OpenApiOperationDocument operationDocument)
            {
                operationDocuments.Add(operationDocument);
            }
        }

        var validator = new OpenApiDocumentValidator();
        var context = new OpenApiValidationContext();

        // TODO: We need a queue mechanism here that resolves dependencies between fragment definitions
        foreach (var (_, fragmentDocument) in fragmentDocumentLookup)
        {
            await validator.ValidateAsync(fragmentDocument, context, cancellationToken).ConfigureAwait(false);

            context.AddValidDocument(fragmentDocument);
        }

        foreach (var operationDocument in operationDocuments)
        {
            await validator.ValidateAsync(operationDocument, context, cancellationToken).ConfigureAwait(false);

            context.AddValidDocument(operationDocument);
        }

        transformer.AddDocuments(
            context.ValidOperationDocuments,
            context.ValidFragmentDocuments,
            schema);

        var endpoints = new List<Endpoint>();

        foreach (var operationDocument in operationDocuments)
        {
            var endpointDescriptor = CreateEndpointDescriptor(operationDocument, fragmentDocumentLookup);

            endpoints.Add(CreateEndpoint(schema.Name, endpointDescriptor));
        }

        dynamicEndpointDataSource.SetEndpoints(endpoints);
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
        Dictionary<string, OpenApiFragmentDocument> fragmentDocumentLookup)
    {
        var definitions = new List<IExecutableDefinitionNode>();
        definitions.Add(operationDocument.OperationDefinition);

        foreach (var referencedFragmentName in operationDocument.ExternalFragmentReferences)
        {
            var fragmentDocument = fragmentDocumentLookup[referencedFragmentName];

            definitions.Add(fragmentDocument.FragmentDefinition);
        }

        var document = new DocumentNode(definitions);

        var rootField = operationDocument.OperationDefinition.SelectionSet.Selections
            .OfType<FieldNode>()
            .First();

        var responseNameToExtract = rootField.Alias?.Value ?? rootField.Name.Value;

        var route = CreateRoutePattern(operationDocument.Route);

        return new OpenApiEndpointDescriptor(
            document,
            operationDocument.HttpMethod,
            route,
            operationDocument.Route.Parameters.ToList(),
            operationDocument.QueryParameters,
            operationDocument.BodyParameter,
            responseNameToExtract);
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

internal sealed record OpenApiEndpointDescriptor(
    DocumentNode Document,
    string HttpMethod,
    RoutePattern Route,
    IReadOnlyList<OpenApiRouteSegmentParameter> RouteParameters,
    IReadOnlyList<OpenApiRouteSegmentParameter> QueryParameters,
    OpenApiRouteSegmentParameter? BodyParameter,
    string ResponseNameToExtract);
