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
using HotChocolate.Types.Introspection;
using Newtonsoft.Json;

namespace HotChocolate.Stitching.Introspection
{
    public static class IntrospectionClient
    {
        private const string _resourceNamespace =
            "HotChocolate.Stitching.Resources";
        private const string _phase1 = "introspection_phase_1.graphql";
        private const string _phase2 = "introspection_phase_2.graphql";

        private const string _schemaName = "__Schema";
        private const string _directiveName = "__Directive";
        private const string _locations = "locations";
        private const string _isRepeatable = "isRepeatable";
        private const string _onOperation = "onOperation";
        private const string _onFragment = "onFragment";
        private const string _onField = "onField";
        private const string _directivesField = "directives";
        private const string _subscriptionType = "subscriptionType";

        public static DocumentNode LoadSchema(
            HttpClient httpClient)
        {
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

        public static DocumentNode RemoveBuiltInTypes(DocumentNode schema)
        {
            var definitions = new List<IDefinitionNode>();

            foreach (IDefinitionNode definition in schema.Definitions)
            {
                if (definition is INamedSyntaxNode type)
                {
                    if (!IntrospectionTypes.IsIntrospectionType(
                        type.Name.Value)
                        && !Types.Scalars.IsBuiltIn(type.Name.Value))
                    {
                        definitions.Add(definition);
                    }
                }
                else if (definition is DirectiveDefinitionNode directive)
                {
                    if (!Types.Directives.IsBuiltIn(directive.Name.Value))
                    {
                        definitions.Add(definition);
                    }
                }
                else
                {
                    definitions.Add(definition);
                }
            }

            return new DocumentNode(definitions);
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
                Query = GetIntrospectionQuery(_phase1),
                OperationName = "introspection_phase_1",
            };

            (string json, HttpResponseMessage _) response =
                await queryClient.FetchStringAsync(
                    request, httpClient)
                .ConfigureAwait(false);

            IntrospectionResult result =
                JsonConvert.DeserializeObject<IntrospectionResult>(
                    response.json);

            FullType directive = result.Data.Schema.Types.First(t =>
                 t.Name.Equals(_directiveName, StringComparison.Ordinal));
            features.HasRepeatableDirectives = directive.Fields.Any(t =>
                t.Name.Equals(_isRepeatable, StringComparison.Ordinal));
            features.HasDirectiveLocations = directive.Fields.Any(t =>
                t.Name.Equals(_locations, StringComparison.Ordinal));

            FullType schema = result.Data.Schema.Types.First(t =>
                 t.Name.Equals(_schemaName, StringComparison.Ordinal));
            features.HasSubscriptionSupport = schema.Fields.Any(t =>
                t.Name.Equals(_subscriptionType, StringComparison.Ordinal));

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
                Query = QuerySyntaxSerializer.Serialize(query),
                OperationName = "introspection_phase_2"
            };

            (string json, HttpResponseMessage _) response =
                await queryClient.FetchStringAsync(
                    request, httpClient)
                .ConfigureAwait(false);

            return IntrospectionDeserializer.Deserialize(response.json);
        }

        internal static DocumentNode CreateIntrospectionQuery(
            SchemaFeatures features)
        {
            DocumentNode query = Utf8GraphQLParser.Parse(
                GetIntrospectionQuery(_phase2));

            OperationDefinitionNode operation =
                query.Definitions.OfType<OperationDefinitionNode>().First();

            FieldNode schema =
                operation.SelectionSet.Selections.OfType<FieldNode>().First();

            FieldNode directives =
                schema.SelectionSet.Selections.OfType<FieldNode>().First(t =>
                    t.Name.Value.Equals(_directivesField,
                        StringComparison.Ordinal));

            var selections = directives.SelectionSet.Selections.ToList();
            AddDirectiveFeatures(features, selections);

            FieldNode newField = directives.WithSelectionSet(
                directives.SelectionSet.WithSelections(selections));

            selections = schema.SelectionSet.Selections.ToList();
            RemoveSubscriptionIfNotSupported(features, selections);

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

        private static void AddDirectiveFeatures(
            SchemaFeatures features,
            ICollection<ISelectionNode> selections)
        {
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
        }

        private static void RemoveSubscriptionIfNotSupported(
            SchemaFeatures features,
            ICollection<ISelectionNode> selections)
        {
            if (!features.HasSubscriptionSupport)
            {
                FieldNode subscriptionField = selections.OfType<FieldNode>()
                    .First(t => t.Name.Value.Equals(_subscriptionType,
                        StringComparison.Ordinal));
                selections.Remove(subscriptionField);
            }
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

            if (stream != null)
            {
                try
                {
                    var buffer = new byte[stream.Length];
                    if (stream.Read(buffer, 0, buffer.Length) > 0)
                    {
                        return Encoding.UTF8.GetString(buffer);
                    }
                }
                finally
                {
                    stream.Dispose();
                }
            }

            return null;
        }
    }
}
