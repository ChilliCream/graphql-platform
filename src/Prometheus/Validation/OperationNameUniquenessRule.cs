using System;
using System.Collections.Generic;
using System.Linq;
using Prometheus.Abstractions;

namespace Prometheus.Validation
{
    public class OperationNameUniquenessRule
        : IQueryValidationRule
    {
        public string Code { get; } = "Q5111";
        public string Description { get; } = "Each named operation definition "
            + "must be unique within a document when referred to by its name.";

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

            HashSet<string> operationNames = new HashSet<string>();
            HashSet<string> failedOperationNames = new HashSet<string>();

            foreach (var operation in queryDocument.OfType<OperationDefinition>())
            {
                if (operationNames.Contains(operation.Name)
                    && !failedOperationNames.Contains(operation.Name))
                {
                    failedOperationNames.Add(operation.Name);
                    yield return new ErrorResult(this,
                        $"The operation name {operation.Name} is not unique.");
                }
                operationNames.Add(operation.Name);
            }

            if (!failedOperationNames.Any())
            {
                yield return new SuccessResult(this);
            }
        }
    }
}