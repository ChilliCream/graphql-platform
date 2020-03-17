using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate;
using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Analyzers.Types;
using StrawberryShake.CodeGeneration.Utilities;
using StrawberryShake.Utilities;
using FieldSelection = StrawberryShake.CodeGeneration.Utilities.FieldSelection;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public partial class DocumentAnalyzer
    {
        private readonly List<DocumentNode> _documents = new List<DocumentNode>();
        private ISchema? _schema;
        private IDocumentHashProvider? _hashProvider;

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

        public DocumentAnalyzer SetHashProvider(IDocumentHashProvider hashProvider)
        {
            _hashProvider = hashProvider;
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

            var context = new DocumentAnalyzerContext(_schema);

            CollectEnumTypes(context, _documents);
            CollectInputObjectTypes(context, _documents);
            CollectOutputTypes(context, _documents);

            return new ClientModel(
                _documents.Select(d => CreateDocumentModel(context, d, _hashProvider)).ToArray(),
                context.Types.ToArray());
        }


        private static DocumentModel CreateDocumentModel(
            IDocumentAnalyzerContext context,
            DocumentNode original,
            IDocumentHashProvider hashProvider)
        {
            DocumentNode optimized = TypeNameQueryRewriter.Rewrite(original);

            string serialized = QuerySyntaxSerializer.Serialize(optimized, false);
            byte[] buffer = Encoding.UTF8.GetBytes(serialized);
            string hash = hashProvider.ComputeHash(buffer);

            return new DocumentModel(
                context.Operations.Where(t => t.Document == original).ToArray(),
                original,
                optimized,
                buffer,
                hashProvider.Name,
                hash);
        }
    }
}
