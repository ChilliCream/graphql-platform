using System;
using System.Collections.Generic;
using Prometheus.Abstractions;
using Prometheus.Resolvers;

namespace Prometheus.Execution
{
    public partial class VariableCollection
        : IVariableCollection
    {
        private readonly ISchemaDocument _schema;
        private readonly OperationDefinition _operation;
        private readonly IDictionary<string, IValue> _variableValues;

        public VariableCollection(ISchemaDocument schema,
            OperationDefinition operation,
            IDictionary<string, object> variableValues)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            _schema = schema;
            _operation = operation;
            _variableValues = CoerceVariableValues(
                schema, operation, variableValues);
        }

        public T GetVariable<T>(string variableName)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                throw new ArgumentException(
                    "variableName mustn't be null or string.Empty.",
                    nameof(variableName));
            }

            IValue value = _variableValues[variableName];
            return ValueConverter.Convert<T>(value);
        }
    }
}