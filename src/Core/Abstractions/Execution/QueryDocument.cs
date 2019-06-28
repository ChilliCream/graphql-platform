using System;
using System.IO;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public class QueryDocument
        : IQuery
    {
        public QueryDocument(DocumentNode document)
        {
            Document = document
                ?? throw new ArgumentNullException(nameof(document));
        }

        public DocumentNode Document { get; }

        public ReadOnlySpan<byte> ToSource()
        {
            using (var stream = new MemoryStream())
            {
                using (var sw = new StreamWriter(stream))
                {
                    QuerySyntaxSerializer.Serialize(Document, sw, false);
                }
                return stream.ToArray();
            }
        }

        public override string ToString() =>
            QuerySyntaxSerializer.Serialize(Document, false);
    }
}
