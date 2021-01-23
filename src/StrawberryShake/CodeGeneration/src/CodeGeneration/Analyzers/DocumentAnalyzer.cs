using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using static StrawberryShake.CodeGeneration.Utilities.OperationDocumentHelper;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public partial class DocumentAnalyzer
    {
        private readonly List<DocumentNode> _documents = new();
        private ISchema? _schema;

        public static DocumentAnalyzer New() => new DocumentAnalyzer();

        public DocumentAnalyzer SetSchema(ISchema schema)
        {
            _schema = schema;
            return this;
        }

        public DocumentAnalyzer AddDocument(DocumentNode document)
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
            Dictionary<NameString, LeafTypeModel> leafTypes = new();
            Dictionary<NameString, InputObjectTypeModel> inputObjectType = new();

            foreach (var operation in operationDocuments.Operations.Values)
            {
                var context = new DocumentAnalyzerContext(_schema, operation);
                OperationModel operationModel = CreateOperationModel(context);
                operations.Add(operationModel);

                foreach (var typeModel in context.TypeModels)
                {
                    if (typeModel is LeafTypeModel leafTypeModel &&
                        !leafTypes.ContainsKey(leafTypeModel.Name))
                    {
                        leafTypes.Add(leafTypeModel.Name, leafTypeModel);
                    }
                    else if (typeModel is InputObjectTypeModel inputObjectTypeModel &&
                        !inputObjectType.ContainsKey(inputObjectTypeModel.Name))
                    {
                        inputObjectType.Add(inputObjectTypeModel.Name, inputObjectTypeModel);
                    }
                }
            }

            return new ClientModel(
                _schema,
                operations,
                leafTypes.Values.ToList(),
                inputObjectType.Values.ToList());
        }
    }
}
