using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public static class DocumentHelper
    {
        public static IEnumerable<(string file, DocumentNode document)> GetTypeSystemDocuments(
            this IEnumerable<(string file, DocumentNode document)> documentNodes)
        {
            if (documentNodes is null)
            {
                throw new ArgumentNullException(nameof(documentNodes));
            }

            return documentNodes.Where(doc =>
                doc.Item2.Definitions.All(def =>
                    def is ITypeSystemDefinitionNode or ITypeSystemExtensionNode));
        }

        public static IEnumerable<(string file, DocumentNode document)> GetExecutableDocuments(
            this IEnumerable<(string  file, DocumentNode document)> documentNodes)
        {
            if (documentNodes is null)
            {
                throw new ArgumentNullException(nameof(documentNodes));
            }

            return documentNodes.Where(doc =>
                doc.Item2.Definitions.All(def => def is IExecutableDefinitionNode));
        }
    }
}
