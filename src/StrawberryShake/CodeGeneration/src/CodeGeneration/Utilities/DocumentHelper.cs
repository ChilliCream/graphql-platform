using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Utilities
{
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
