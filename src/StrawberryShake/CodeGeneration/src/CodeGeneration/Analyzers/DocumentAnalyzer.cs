using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Analyzers.Types;
using StrawberryShake.CodeGeneration.Utilities;
using FieldSelection = StrawberryShake.CodeGeneration.Utilities.FieldSelection;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public partial class DocumentAnalyzer
    {
        private readonly List<DocumentNode> _documents = new List<DocumentNode>();
        private ISchema? _schema;

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

        public IClientModel Analyze()
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

            var context = new DocumentAnalyzerContext(_schema);

            CollectEnumTypes(context, _documents);
            CollectInputObjectTypes(context, _documents);
            CollectOutputTypes(context, _documents);

            throw new NotImplementedException();
        }
    }
}
