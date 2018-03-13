using System;
using System.Collections.Generic;
using System.Linq;
using Prometheus.Abstractions;

namespace Prometheus.Validation
{
    public class LoneAnonymousOperationRule
        : IQueryValidationRule
    {
        public string Code { get; } = "Q5121";

        public string Description { get; } = "GraphQL allows a short‐hand "
            + "form for defining query operations when only that one "
            + "operation exists in the document.";

        public IEnumerable<IValidationResult> Apply(
            ISchemaDocument schemaDocument,
            IQueryDocument queryDocument)
        {
            if (schemaDocument == null)
            {
                throw new ArgumentNullException(nameof(schemaDocument));
            }

            if (queryDocument == null)
            {
                throw new ArgumentNullException(nameof(queryDocument));
            }

            OperationDefinition[] operations = queryDocument
                .OfType<OperationDefinition>().ToArray();

            if (operations.Length > 1
                && operations.Any(t => string.IsNullOrEmpty(t.Name)))
            {
                yield return new ErrorResult(this, "There is at least one "
                    + " anonymous operation although the query consists of "
                    + "more than one operation.");
            }
            else
            {
                yield return new SuccessResult(this);
            }
        }
    }
}