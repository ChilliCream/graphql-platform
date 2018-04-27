using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class ArgumentResolver
    {
        public void CoerceArgumentValues(
            ObjectType objectType,
            FieldSelection fieldSelection,
            VariableCollection variables)
        {
            Dictionary<string, CoercedValue> dictionary =
                new Dictionary<string, CoercedValue>();

            foreach (InputField argument in fieldSelection.Field.Arguments.Values)
            {
                string variableName = argument.Name;
                IInputType variableType = argument.Type;
                IValueNode defaultValue = argument.DefaultValue;
                // IValueNode value = fieldSelection.Node.Arguments

                /*
                                if()

                                if (!variableValues.TryGetValue(variableName,
                                    out IValueNode variableValue))
                                {
                                    variableValue = defaultValue;
                                }

                                if (type.IsNonNullType() && IsNulValue(variableValue))
                                {
                                    errors.Add(new VariableError(
                                        "The variable value cannot be null.",
                                        variableName));
                                }

                                if (!type.IsInstanceOfType(variableValue))
                                {
                                    errors.Add(new VariableError(
                                        "The variable value is not of the variable type.",
                                        variableName));
                                }

                                coercedValues[variableName] =
                                        new CoercedValue(type, variableValue);

*/
            }
        }
    }
}
