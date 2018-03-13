using System.Collections.Generic;
using Prometheus.Abstractions;

namespace Prometheus.Validation
{
    public class FieldSelectionMustBeDefinedRule
        : IQueryValidationRule
    {
        public string Code { get; } = "Q521";

        public string Description { get; } = "The target field of a field "
            + "selection must be defined on the scoped type of the "
            + "selection set. There are no limitations on alias names.";

        public IEnumerable<IValidationResult> Apply(
            ISchemaDocument schemaDocument,
            IQueryDocument queryDocument)
        {
            yield break;
        }

        private IEnumerable<IHasSelectionSet> GetNodesWithSelectionSet()
        {
            yield break;
        }
    }
}