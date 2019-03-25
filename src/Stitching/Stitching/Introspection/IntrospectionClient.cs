using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Stitching.Introspection.Models;
using HotChocolate.Stitching.Properties;
using HotChocolate.Stitching.Utilities;
using Newtonsoft.Json;

namespace HotChocolate.Stitching.Introspection
{
    public static class IntrospectionClient
    {
        private const string _directiveName = "__Directive";
        private const string _locations = "locations";
        private const string _isRepeatable = "isRepeatable";
        private const string _onOperation = "onOperation";
        private const string _onFragment = "onFragment";
        private const string _onField = "onField";
        private const string _directivesField = "directives";

        public static DocumentNode LoadSchema(
            HttpClient httpClient)
        {
            var queryClient = new HttpQueryClient();
            return Task.Factory.StartNew(
                () => LoadSchemaAsync(httpClient))
                .Unwrap().GetAwaiter().GetResult();
        }

        public static Task<DocumentNode> LoadSchemaAsync(
            HttpClient httpClient)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            return LoadSchemaInternalAsync(httpClient);
        }

        private static async Task<DocumentNode> LoadSchemaInternalAsync(
            HttpClient httpClient)
        {
            var queryClient = new HttpQueryClient();

            SchemaFeatures features =
                await GetSchemaFeaturesAsync(httpClient, queryClient)
                    .ConfigureAwait(false);
            return await ExecuteIntrospectionAsync(
                httpClient, queryClient, features)
                .ConfigureAwait(false);
        }

        private static async Task<SchemaFeatures> GetSchemaFeaturesAsync(
            HttpClient httpClient,
            HttpQueryClient queryClient)
        {
            var features = new SchemaFeatures();

            var request = new HttpQueryRequest
            {
                Query = Encoding.UTF8.GetString(
                    StitchingResources.IntrospectionPhase1)
            };

            string json = await queryClient.FetchStringAsync(
                request, httpClient)
                .ConfigureAwait(false);

            IntrospectionResult result =
                JsonConvert.DeserializeObject<IntrospectionResult>(json);

            FullType type = result.Data.Schema.Types.First(t =>
                 t.Name.Equals(_directiveName, StringComparison.Ordinal));
            features.HasRepeatableDirectives = type.Fields.Any(t =>
                t.Name.Equals(_isRepeatable, StringComparison.Ordinal));
            features.HasDirectiveLocations = type.Fields.Any(t =>
                t.Name.Equals(_locations, StringComparison.Ordinal));
            return features;
        }

        private static async Task<DocumentNode> ExecuteIntrospectionAsync(
            HttpClient httpClient,
            HttpQueryClient queryClient,
            SchemaFeatures features)
        {
            DocumentNode query = CreateIntrospectionQuery(features);

            var request = new HttpQueryRequest
            {
                Query = QuerySyntaxSerializer.Serialize(query)
            };

            string json = await queryClient.FetchStringAsync(
                request, httpClient)
                .ConfigureAwait(false);

            return IntrospectionDeserializer.Deserialize(json);
        }

        private static DocumentNode CreateIntrospectionQuery(
            SchemaFeatures features)
        {
            DocumentNode query = Parser.Default.Parse(
                Encoding.UTF8.GetString(
                    StitchingResources.IntrospectionPhase2));

            OperationDefinitionNode operation =
                query.Definitions.OfType<OperationDefinitionNode>().First();

            FieldNode schema =
                operation.SelectionSet.Selections.OfType<FieldNode>().First();

            FieldNode directives =
                schema.SelectionSet.Selections.OfType<FieldNode>().First(t =>
                    t.Name.Value.Equals(_directivesField
                        , StringComparison.Ordinal));

            var selections = directives.SelectionSet.Selections.ToList();

            if (features.HasDirectiveLocations)
            {
                selections.Add(CreateField(_locations));
            }
            else
            {
                selections.Add(CreateField(_onField));
                selections.Add(CreateField(_onFragment));
                selections.Add(CreateField(_onOperation));
            }

            if (features.HasRepeatableDirectives)
            {
                selections.Add(CreateField(_isRepeatable));
            }

            FieldNode newField = directives.WithSelectionSet(
                directives.SelectionSet.WithSelections(selections));

            selections = schema.SelectionSet.Selections.ToList();
            selections.Remove(directives);
            selections.Add(newField);

            newField = schema.WithSelectionSet(
                schema.SelectionSet.WithSelections(selections));

            selections = operation.SelectionSet.Selections.ToList();
            selections.Remove(schema);
            selections.Add(newField);

            OperationDefinitionNode newOp = operation.WithSelectionSet(
                operation.SelectionSet.WithSelections(selections));

            var definitions = query.Definitions.ToList();
            definitions.Remove(operation);
            definitions.Insert(0, newOp);

            return query.WithDefinitions(definitions);
        }

        private static FieldNode CreateField(string name) =>
            new FieldNode(null, new NameNode(name), null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                null);
    }
}
