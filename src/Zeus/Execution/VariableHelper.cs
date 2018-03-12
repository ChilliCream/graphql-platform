using System;
using System.Collections.Generic;
using System.Linq;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    internal static class VariableHelper
    {
        public static QueryResult CoerceVariableValues(
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
                throw new ArgumentNullException(nameof(variableValues));
            }

            Dictionary<string, object> coercedValues = new Dictionary<string, object>();
            List<QueryError> errors = new List<QueryError>();

            foreach (VariableDefinition variable in operation.VariableDefinitions.Values)
            {
                object value = GetVariableValue(variable, variableValues, errors);

                if (!IsVariableValueValid(schema, variable.Type, value))
                {
                    errors.Add(new QueryError(
                       $"The value of {variable.Name} "
                       + "is not of the type {variable.Type}."));

                }

                coercedValues[variable.Name] = value;
            }

            return errors.Any()
                ? new QueryResult(errors)
                : new QueryResult(coercedValues);
        }

        private static object GetVariableValue(
            VariableDefinition variable,
            IDictionary<string, object> variableValues,
            ICollection<QueryError> errors)
        {
            if (!variableValues.TryGetValue(variable.Name, out var value))
            {
                if (variable.Type.IsNonNullType() && variable.DefaultValue == null)
                {
                    errors.Add(new QueryError($"Variable {variable.Name} mustn't be null."));
                }

                if (variable.DefaultValue != null)
                {
                    value = ValueConverter.Convert(variable.DefaultValue, typeof(object));
                }
            }

            return value;
        }

        private static bool IsVariableValueValid(ISchemaDocument schema, IType expectedType, object value)
        {
            return true;
        }
    }

    internal class InputObjectValidator
    {

    }
}