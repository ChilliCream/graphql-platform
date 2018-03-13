using System.Collections.Generic;
using Prometheus.Abstractions;

namespace Prometheus.Parser
{
    public interface ISchemDocumentReader
    {
        SchemaDocument Read(IEnumerable<string> schemas);
    }

    public static class SchemaDocumentReaderExtensions
    {
        public static SchemaDocument Read(this ISchemDocumentReader schemDocumentReader, params string[] schemas)
        {
            return schemDocumentReader.Read(schemas);
        }
    }
}
