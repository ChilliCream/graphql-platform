using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate;
using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using static StrawberryShake.CodeGeneration.Utilities.OperationDocumentHelper;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public partial class DocumentAnalyzer
    {
        private readonly Dictionary<string, DocumentNode> _documents = new();
        private readonly HashSet<string> _reservedName = new();
        private ISchema? _schema;
        private IDocumentHashProvider? _hashProvider;

        public DocumentAnalyzer SetSchema(ISchema schema)
        {
            _schema = schema;
            return this;
        }

        public DocumentAnalyzer AddDocument(string name, DocumentNode document)
        {
            _documents.Add(name, document);
            return this;
        }

        public DocumentAnalyzer SetHashProvider(IDocumentHashProvider hashProvider)
        {
            _hashProvider = hashProvider;
            return this;
        }

        public DocumentAnalyzer AddReservedName(string name)
        {
            _reservedName.Add(name);
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

            if (_hashProvider is null)
            {
                throw new InvalidOperationException(
                    "You must specify a hash provider.");
            }

            var context = new DocumentAnalyzerContext(_schema, _reservedName);
            OperationDocuments operations = CreateOperationDocuments(_documents.Values);

            CollectEnumTypes(context, operations.Document);
            CollectInputObjectTypes(context, operations.Document);
            CollectOutputTypes(context, operations.Document);

            return new ClientModel(
                _documents
                    .Select(d => CreateDocumentModel(context, d.Key, d.Value, _hashProvider))
                    .ToArray(),
                context.Types.ToArray());
        }


        private static DocumentModel CreateDocumentModel(
            IDocumentAnalyzerContext context,
            string name,
            DocumentNode original,
            IDocumentHashProvider hashProvider)
        {
            DocumentNode optimized = TypeNameQueryRewriter.Rewrite(original);

            throw new Exception();

            string serialized = null; //  QuerySyntaxSerializer.Serialize(optimized, false);
            byte[] buffer = Encoding.UTF8.GetBytes(serialized);
            string hash = hashProvider.ComputeHash(buffer);

            return new DocumentModel(
                name,
                context.Operations.Where(t => t.Document == original).ToArray(),
                original,
                optimized,
                buffer,
                hashProvider.Name,
                hash);
        }
    }
}
