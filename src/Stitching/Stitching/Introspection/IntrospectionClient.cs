using System;
using System.Collections.Generic;
using System.IO;
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
        private const string _resourceNamespace = "HotChocolate.Stitching.Resources";
        private const string _phase1 = "introspection_phase_1.graphql";
        private const string _phase2 = "introspection_phase_2.graphql";

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
                Query = GetIntrospectionQuery(_phase1)
            };

            string json = await queryClient.FetchStringAsync(request, httpClient)
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

            string json = await queryClient.FetchStringAsync(request, httpClient)
                .ConfigureAwait(false);

            return IntrospectionDeserializer.Deserialize(json);
        }

        private static DocumentNode CreateIntrospectionQuery(SchemaFeatures features)
        {
            DocumentNode query = Parser.Default.Parse(GetIntrospectionQuery(_phase2));

            OperationDefinitionNode operation =
                query.Definitions.OfType<OperationDefinitionNode>().First();

            FieldNode schema =
                operation.SelectionSet.Selections.OfType<FieldNode>().First();

            FieldNode directives =
                schema.SelectionSet.Selections.OfType<FieldNode>().First(t =>
                    t.Name.Value.Equals(_directivesField, StringComparison.Ordinal));

            var selections = new List<ISelectionNode>(directives.SelectionSet.Selections);

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

            selections = new List<ISelectionNode>(schema.SelectionSet.Selections);
            selections.Remove(directives);
            selections.Add(newField);

            newField = schema.WithSelectionSet(
                schema.SelectionSet.WithSelections(selections));

            selections = new List<ISelectionNode>(operation.SelectionSet.Selections);
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

        private static string GetIntrospectionQuery(string fileName)
        {
            Stream stream = typeof(IntrospectionClient).Assembly
                .GetManifestResourceStream($"{_resourceNamespace}.{fileName}");

            if (stream == null)
            {
                return null;
            }

            try
            {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer);
            }
            finally
            {
                stream.Dispose();
            }
        }
    }
}
