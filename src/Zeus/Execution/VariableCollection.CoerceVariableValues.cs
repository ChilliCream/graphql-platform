using System;
using System.Collections.Generic;
using System.Linq;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    public partial class VariableCollection
    {
        private static IDictionary<string, IValue> CoerceVariableValues(
            ISchemaDocument schema,
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

            if (variableValues == null)
            {
                variableValues = new Dictionary<string, object>();
            }

            Dictionary<string, IValue> coercedValues = new Dictionary<string, IValue>();
            List<string> errors = new List<string>();

            foreach (VariableDefinition variable in operation.VariableDefinitions.Values)
            {
                coercedValues[variable.Name] = GetVariableValue(
                    schema, variable, variableValues, errors);
            }

            if (errors.Any())
            {
                throw new GraphQLQueryException(errors.ToArray());
            }

            return coercedValues;
        }

        private static IValue GetVariableValue(
            ISchemaDocument schema,
            VariableDefinition variable,
            IDictionary<string, object> variableValues,
            ICollection<string> errors)
        {
            if (!variableValues.TryGetValue(variable.Name, out var value))
            {
                if (variable.Type.IsNonNullType() && variable.DefaultValue == null)
                {
                    errors.Add($"Variable {variable.Name} mustn't be null.");
                }

                if (variable.DefaultValue == null)
                {
                    return NullValue.Instance;
                }

                return variable.DefaultValue;
            }

            return ValueConverter.Convert(value, schema, variable.Type);
        }
    }
}