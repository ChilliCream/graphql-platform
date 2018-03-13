using System;
using Prometheus.Abstractions;
using Prometheus.Introspection;
using Prometheus.Resolvers;

namespace Prometheus.Execution
{
    internal class OptimizedSelectionHelper
    {
        private readonly OperationContext _operationContext;
        private readonly string _typeName;

        public OptimizedSelectionHelper(OperationContext operationContext, string typeName)
        {
            _operationContext = operationContext
                ?? throw new ArgumentNullException(nameof(operationContext));

            _typeName = typeName
                ?? throw new ArgumentNullException(nameof(typeName));
        }

        public bool TryCreateSelectionContext(Field field, out SelectionContext selectionContext)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (_operationContext.Schema.ObjectTypes.TryGetValue(_typeName, out var typeDefinition))
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