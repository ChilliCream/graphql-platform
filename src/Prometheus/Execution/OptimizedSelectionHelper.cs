using System;
using Prometheus.Abstractions;
using Prometheus.Introspection;
using Prometheus.Resolvers;

namespace Prometheus.Execution
{
    internal class OptimizedSelectionHelper
    {
        private readonly OperationContext _operationContext;

        public OptimizedSelectionHelper(OperationContext operationContext)
        {
            _operationContext = operationContext
                ?? throw new ArgumentNullException(nameof(operationContext));
        }

        public bool TryCreateSelectionContext(
            string typeName, Field field, 
            out SelectionContext selectionContext)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            ISchema schema = _operationContext.Schema;
            if (schema.ObjectTypes.TryGetValue(typeName, out var typeDefinition))
            {
                if (typeDefinition.Fields.TryGetValue(field.Name, out var fieldDefinition))
                {
                    selectionContext = new SelectionContext(typeDefinition, fieldDefinition, field);
                    return true;
                }
            }

            selectionContext = null;
            return false;
        }
    }
}