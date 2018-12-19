using System;
using System.IO;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public class AnnotationQueryParser
        : IQueryParser
    {
        private readonly AnnotateQueryRewriter _rewriter =
            new AnnotateQueryRewriter();
        private readonly ISchema _schema;

        public AnnotationQueryParser(ISchema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        public DocumentNode Parse(string queryText)
        {
            return _rewriter.Rewrite<DocumentNode, AnnotationContext>(
                Parser.Default.Parse(queryText),
                AnnotationContext.Create(_schema));
        }
    }
}
