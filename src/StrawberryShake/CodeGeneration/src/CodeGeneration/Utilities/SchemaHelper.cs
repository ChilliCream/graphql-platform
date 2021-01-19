using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Analyzers;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public static class SchemaHelper
    {
        public static ISchema Load(params DocumentNode[] documents)
        {
            return Load((IEnumerable<DocumentNode>)documents);
        }

        public static ISchema Load(IEnumerable<DocumentNode> documents)
        {
            if (documents is null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            ISchemaBuilder builder = SchemaBuilder.New();
            var scalarInfos = new Dictionary<NameString, ScalarInfo>();

            foreach (DocumentNode document in documents)
            {
                if (document.Definitions.Any(
                    t => t is ScalarTypeExtensionNode or EnumTypeExtensionNode))
                {
                    CollectScalarInfos(
                        document.Definitions.OfType<ScalarTypeExtensionNode>(),
                        scalarInfos);
                }
                else
                {
                    builder.AddDocument(document);
                }
            }

            return builder
                .TryAddTypeInterceptor(new LeafTypeInterceptor(scalarInfos))
                .Use(next => context => throw new NotSupportedException())
                .Create();
        }

        private static void CollectScalarInfos(
            IEnumerable<ScalarTypeExtensionNode> scalarTypeExtensions,
            Dictionary<NameString, ScalarInfo> scalarInfos)
        {
            foreach (ScalarTypeExtensionNode scalarTypeExtension in scalarTypeExtensions)
            {
                if (!scalarInfos.TryGetValue(
                    scalarTypeExtension.Name.Value,
                    out ScalarInfo scalarInfo))
                {
                    scalarInfo = new ScalarInfo(
                        scalarTypeExtension.Name.Value,
                        GetDirectiveValue(scalarTypeExtension, "runtimeType"),
                        GetDirectiveValue(scalarTypeExtension, "serializationType"));
                    scalarInfos.Add(scalarInfo.TypeName, scalarInfo);
                }
            }
        }

        private static string? GetDirectiveValue(
            IHasDirectives hasDirectives,
            NameString directiveName)
        {
            DirectiveNode? directive =
                hasDirectives.Directives.FirstOrDefault(t => directiveName.Equals(t.Name.Value));

            if (directive is { Arguments: { Count: > 0 } })
            {
                ArgumentNode? argument =
                    directive.Arguments.FirstOrDefault(t => t.Name.Value.Equals("name"));

                if (argument is { Value: StringValueNode stringValue })
                {
                    return stringValue.Value;
                }
            }

            return null;
        }
    }

    public static class DocumentHelper
    {
        public static IEnumerable<DocumentNode> GetTypeSystemDocuments(
            this IEnumerable<DocumentNode> documentNodes)
        {
            if (documentNodes is null)
            {
                throw new ArgumentNullException(nameof(documentNodes));
            }

            return documentNodes.Where(doc =>
                doc.Definitions.All(def =>
                    def is ITypeSystemDefinitionNode or ITypeSystemExtensionNode));
        }

        public static IEnumerable<DocumentNode> GetExecutableDocuments(
            this IEnumerable<DocumentNode> documentNodes)
        {
            if (documentNodes is null)
            {
                throw new ArgumentNullException(nameof(documentNodes));
            }

            return documentNodes.Where(doc =>
                doc.Definitions.All(def => def is IExecutableDefinitionNode));
        }
    }
}
