using System.Collections.Generic;
using Prometheus.Abstractions;

namespace Prometheus.Validation
{
    public interface IQueryValidationRule
        : IValidationRule
    {
        IEnumerable<IValidationResult> Apply(
            ISchemaDocument schemaDocument,
            IQueryDocument queryDocument);
    }
}