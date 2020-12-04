using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Utilities.Introspection
{
    internal static class IntrospectionQueryHelper
    {
        private const string _resourceNamespace = "HotChocolate.Utilities.Introspection.Queries";
        private const string _featureQueryFile = "introspection_phase_1.graphql";
        private const string _introspectionQueryFile = "introspection_phase_2.graphql";
        private const string _onOperation = "onOperation";
        private const string _onFragment = "onFragment";
        private const string _onField = "onField";
        private const string _directivesField = "directives";

        public static HttpQueryRequest CreateFeatureQuery() =>
            new(GetFeatureQuery(), "introspection_phase_1");

        public static HttpQueryRequest CreateIntrospectionQuery(ISchemaFeatures features)
        {
            DocumentNode document = CreateIntrospectionQueryDocument(features);
            string sourceText = document.Print(false);
            return new HttpQueryRequest(sourceText, "introspection_phase_2");
        }

        private static DocumentNode CreateIntrospectionQueryDocument(ISchemaFeatures features)
        {
            DocumentNode query = Utf8GraphQLParser.Parse(GetIntrospectionQuery());

            OperationDefinitionNode operation =
                query.Definitions.OfType<OperationDefinitionNode>().First();

            FieldNode schema =
                operation.SelectionSet.Selections.OfType<FieldNode>().First();

            if (schema.SelectionSet is null)
            {
                throw new IntrospectionException();
            }

            FieldNode directives =
                schema.SelectionSet.Selections.OfType<FieldNode>().First(t =>
                    t.Name.Value.Equals(_directivesField, StringComparison.Ordinal));

            if (directives.SelectionSet is null)
            {
                throw new IntrospectionException();
            }

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
            ISchemaFeatures features,
            ICollection<ISelectionNode> selections)
        {
            if (features.HasDirectiveLocations)
            {
                selections.Add(CreateField(SchemaFeatures.Locations));
            }
            else
            {
                selections.Add(CreateField(_onField));
                selections.Add(CreateField(_onFragment));
                selections.Add(CreateField(_onOperation));
            }

            if (features.HasRepeatableDirectives)
            {
                selections.Add(CreateField(SchemaFeatures.IsRepeatable));
            }
        }

        private static void RemoveSubscriptionIfNotSupported(
            ISchemaFeatures features,
            ICollection<ISelectionNode> selections)
        {
            if (!features.HasSubscriptionSupport)
            {
                FieldNode subscriptionField = selections.OfType<FieldNode>()
                    .First(t => t.Name.Value.Equals(
                        SchemaFeatures.SubscriptionType,
                        StringComparison.Ordinal));
                selections.Remove(subscriptionField);
            }
        }

        private static FieldNode CreateField(string name) =>
            new(null, new NameNode(name), null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                null);

        private static string GetFeatureQuery() => GetQueryFile(_featureQueryFile);

        private static string GetIntrospectionQuery() => GetQueryFile(_introspectionQueryFile);

        private static string GetQueryFile(string fileName)
        {
#pragma warning disable CS8600
            Stream? stream = typeof(IntrospectionClient).Assembly
                .GetManifestResourceStream($"{_resourceNamespace}.{fileName}");
#pragma warning restore CS8600

            if (stream is not null)
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

            throw new IntrospectionException("Could not find query file: " + fileName);
        }
    }
}
