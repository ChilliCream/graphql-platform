using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Utilities
{
    /// <summary>
    /// Merges all documents and create one query document per operation.
    /// </summary>
    internal static class OperationDocumentHelper
    {
        /// <summary>
        /// Merges the documents and creates operation documents that
        /// can be used for the actual requests.
        /// </summary>
        /// <param name="documents">
        /// The GraphQL documents.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static OperationDocuments CreateOperationDocuments(
            IEnumerable<DocumentNode> documents)
        {
            if (documents is null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            DocumentNode mergedDocument = MergeDocuments(documents);
            Dictionary<string, DocumentNode> operationDocs = ExportOperations(mergedDocument);
            return new OperationDocuments(mergedDocument, operationDocs);
        }

        private static DocumentNode MergeDocuments(IEnumerable<DocumentNode> documents)
        {
            var definitions = new List<IDefinitionNode>();

            foreach (DocumentNode document in documents)
            {
                definitions.AddRange(document.Definitions);
            }

            ValidateDocument(definitions);

            return new DocumentNode(definitions);
        }

        private static void ValidateDocument(IEnumerable<IDefinitionNode> definitions)
        {
            var operationNames = new HashSet<string>();
            var fragmentNames = new HashSet<string>();

            foreach (var definition in definitions)
            {
                if (definition is OperationDefinitionNode op)
                {
                    if (op.Name is null)
                    {
                        throw new CodeGeneratorException(
                            ErrorBuilder.New()
                                .SetMessage("All operations must be named.")
                                .AddLocation(op)
                                .Build());
                    }

                    if (!operationNames.Add(op.Name.Value))
                    {
                        throw new CodeGeneratorException(
                            ErrorBuilder.New()
                                .SetMessage(
                                    "The operation name `{0}` is not unique.",
                                    op.Name.Value)
                                .AddLocation(op)
                                .Build());
                    }
                }

                if (definition is FragmentDefinitionNode fd)
                {
                    if (!fragmentNames.Add(fd.Name.Value))
                    {
                        throw new CodeGeneratorException(
                            ErrorBuilder.New()
                                .SetMessage(
                                    "The operation name `{0}` is not unique.",
                                    fd.Name.Value)
                                .AddLocation(fd)
                                .Build());
                    }
                }
            }
        }

        private static Dictionary<string, DocumentNode> ExportOperations(DocumentNode document)
        {
            var visitor = new ExtractOperationVisitor();
            var context = new ExtractOperationContext(document);
            var operationDocs = new Dictionary<string, DocumentNode>();

            do
            {
                visitor.Visit(context.Operation, context);

                var definitions = new List<IDefinitionNode> { context.Operation };
                definitions.AddRange(context.ExportedFragments);
                operationDocs.Add(context.Operation.Name!.Value, new DocumentNode(definitions));
            } while (context.Next());

            return operationDocs;
        }
    }
}
