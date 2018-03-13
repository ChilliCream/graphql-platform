using Prometheus.Abstractions;
using Prometheus.Parser;

namespace Prometheus.Validation
{
    public abstract class QueryValidationTestBase
    {
        protected ISchemaDocument CreateSchema()
        {
            SchemaDocumentReader schemaReader = new SchemaDocumentReader();
            return schemaReader.Read(Schemas.Default);
        }

        protected IQueryDocument ParseQuery(string query)
        {
            QueryDocumentReader queryReader = new QueryDocumentReader();
            return queryReader.Read(query);
        }
    }
}