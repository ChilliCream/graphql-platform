using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Stitching.SchemaDefinitions;
using HotChocolate.Utilities;
using HotChocolate.Utilities.Introspection;
using static HotChocolate.Stitching.Execution.RemoteRequestHelper;

namespace HotChocolate.Stitching.Utilities;

internal sealed class IntrospectionHelper
{
    private readonly HttpClient _httpClient;
    private readonly string _configuration;

    public IntrospectionHelper(HttpClient httpClient, string configuration)
    {
        if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<RemoteSchemaDefinition> GetSchemaDefinitionAsync(
        CancellationToken cancellationToken)
    {
        // The introspection client will do all the hard work to negotiate
        // the features this schema supports and convert the introspection
        // result into a parsed GraphQL SDL document.
        DocumentNode schemaDocument = await new IntrospectionClient()
            .DownloadSchemaAsync(_httpClient, cancellationToken)
            .ConfigureAwait(false);

        // If the down-stream service provides a schema definition we will fetch it.
        if (ProvidesSchemaDefinition(schemaDocument))
        {
            // if a schema definition with the requested configuration name is
            // available we will use it instead of the introspection result.
            RemoteSchemaDefinition? schemaDefinition =
                await FetchSchemaDefinitionAsync(cancellationToken)
                    .ConfigureAwait(false);
            if (schemaDefinition is not null)
            {
                return schemaDefinition;
            }
        }

        // if no schema definition is available on the down-stream services that matches
        // the configuration we will use the introspection result and infer one.
        return new RemoteSchemaDefinition(
            _configuration,
            schemaDocument,
            Array.Empty<DocumentNode>());
    }

    private static bool ProvidesSchemaDefinition(DocumentNode schemaDocument)
    {
        SchemaDefinitionNode? schemaDefinition = schemaDocument.Definitions
            .OfType<SchemaDefinitionNode>().SingleOrDefault();

        OperationTypeDefinitionNode? operation =
            schemaDefinition?.OperationTypes.FirstOrDefault(
                t => t.Operation == OperationType.Query);

        if (operation is null)
        {
            return false;
        }

        ObjectTypeDefinitionNode? queryType = schemaDocument.Definitions
            .OfType<ObjectTypeDefinitionNode>()
            .FirstOrDefault(t => t.Name.Value.EqualsOrdinal(operation.Type.Name.Value));

        if (queryType is null)
        {
            return false;
        }

        FieldDefinitionNode? schemaDefinitionField = queryType.Fields
            .FirstOrDefault(t => t.Name.Value.EqualsOrdinal(
                SchemaDefinitionFieldNames.SchemaDefinitionField));

        return schemaDefinitionField != null &&
            schemaDefinitionField.Arguments.Any(
                t => t.Name.Value.EqualsOrdinal(SchemaDefinitionFieldNames.ConfigurationArgument));
    }

    private async ValueTask<RemoteSchemaDefinition?> FetchSchemaDefinitionAsync(
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();

        IQueryRequest request =
            QueryRequestBuilder.New()
                .SetQuery(
                    $@"query GetSchemaDefinition($c: String!) {{
                            {SchemaDefinitionFieldNames.SchemaDefinitionField}(configuration: $c) {{
                                name
                                document
                                extensionDocuments
                            }}
                        }}")
                .SetVariableValue("c", new StringValueNode(_configuration.Value))
                .Create();

        HttpRequestMessage requestMessage =
            await CreateRequestMessageAsync(writer, request, cancellationToken)
                .ConfigureAwait(false);

        HttpResponseMessage responseMessage = await _httpClient
            .SendAsync(requestMessage, cancellationToken)
            .ConfigureAwait(false);

        IQueryResult result =
            await ParseResponseMessageAsync(responseMessage, cancellationToken)
                .ConfigureAwait(false);

        if (result.Errors is { Count: > 1 })
        {
            throw ThrowHelper.IntrospectionHelper_UnableToFetchSchemaDefinition(result.Errors);
        }

        if (result.Data is not null &&
            result.Data[SchemaDefinitionFieldNames.SchemaDefinitionField]
                is IReadOnlyDictionary<string, object?> o &&
            o.TryGetValue("name", out var n) &&
            n is StringValueNode name &&
            o.TryGetValue("document", out var d) &&
            d is StringValueNode document &&
            o.TryGetValue("extensionDocuments", out var e) &&
            e is IReadOnlyList<object> extensionDocuments)
        {
            return new RemoteSchemaDefinition(
                name.Value,
                Utf8GraphQLParser.Parse(document.Value),
                extensionDocuments
                    .OfType<StringValueNode>()
                    .Select(t => t.Value)
                    .Select(Utf8GraphQLParser.Parse));
        }

        return null;
    }
}
