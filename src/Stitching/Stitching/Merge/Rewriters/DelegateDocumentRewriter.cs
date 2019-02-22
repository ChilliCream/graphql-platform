using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    public delegate DocumentNode RewriteDocumentDelegate(
        ISchemaInfo schema, DocumentNode document);

    internal class DelegateDocumentRewriter
        : IDocumentRewriter
    {
        private readonly RewriteDocumentDelegate _rewrite;

        public DelegateDocumentRewriter(RewriteDocumentDelegate rewrite)
        {
            _rewrite = rewrite
                ?? throw new ArgumentNullException(nameof(rewrite));
        }

        public DocumentNode Rewrite(ISchemaInfo schema, DocumentNode document)
        {
            return _rewrite.Invoke(schema, document);
        }
    }
}
