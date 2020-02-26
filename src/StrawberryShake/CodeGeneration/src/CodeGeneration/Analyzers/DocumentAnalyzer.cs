using System;
using HotChocolate;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration
{
    public class DocumentAnalyzer
    {
        public DocumentAnalyzer SetSchema(ISchema schema)
        {
            return this;
        }

        public DocumentAnalyzer AddDocument(DocumentNode document)
        {
            return this;
        }

        public IDocumentModel Analyze()
        {
            throw new NotImplementedException();
        }
    }

    public interface IDocumentModel
    {

    }
}
