using Prometheus.Abstractions;

namespace Prometheus.Execution
{
    public interface IDocumentValidator
    {
        DocumentValidationReport Validate(ISchema schema, QueryDocument document);
    }
}