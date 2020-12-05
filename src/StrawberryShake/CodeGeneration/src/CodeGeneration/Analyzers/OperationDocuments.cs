using System.Collections.Generic;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal class OperationDocuments
    {
        public OperationDocuments(
            DocumentNode document,
            IReadOnlyDictionary<string, DocumentNode> operations)
        {
            Document = document;
            Operations = operations;
        }

        public DocumentNode Document { get;  }

        public IReadOnlyDictionary<string, DocumentNode> Operations { get; }
    }
}
