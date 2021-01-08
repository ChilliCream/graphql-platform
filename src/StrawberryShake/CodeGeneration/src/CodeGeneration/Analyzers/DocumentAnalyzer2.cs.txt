using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Analyzers.Models2;
using StrawberryShake.CodeGeneration.Utilities;
using static StrawberryShake.CodeGeneration.Utilities.OperationDocumentHelper;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public partial class DocumentAnalyzer2
    {
        private readonly List<DocumentNode> _documents = new();
        private ISchema? _schema;

        public DocumentAnalyzer2 SetSchema(ISchema schema)
        {
            _schema = schema;
            return this;
        }

        public DocumentAnalyzer2 AddDocument(DocumentNode document)
        {
            _documents.Add(document);
            return this;
        }

        public ClientModel Analyze()
        {
            if (_schema is null)
            {
                throw new InvalidOperationException(
                    "You must provide a schema.");
            }

            if (_documents.Count == 0)
            {
                throw new InvalidOperationException(
                    "You must at least provide one document.");
            }

            OperationDocuments operationDocuments = CreateOperationDocuments(_documents);
            List<OperationModel> operations = new();

            foreach (var operation in operationDocuments.Operations.Values)
            {

                // CollectOutputTypes(context, operations.Document);
            }

            return new ClientModel(
                operations,
                CollectEnumTypes(_schema, operationDocuments.Document),
                CollectInputObjectTypes(_schema, operationDocuments.Document));
        }
    }
}
