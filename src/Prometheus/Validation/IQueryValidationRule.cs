using System.Collections.Generic;
using Prometheus.Abstractions;

namespace Prometheus.Validation
{
    public interface IValidationRule
    {
        string Code { get; }
        string Description { get; }
    }

    public interface IQueryValidationRule
        : IValidationRule
    {
        IEnumerable<IValidationResult> Apply(
            ISchemaDocument schemaDocument,
            IQueryDocument queryDocument);
    }

    public interface IValidationResult
    {
        IValidationRule Rule { get; }
    }
}