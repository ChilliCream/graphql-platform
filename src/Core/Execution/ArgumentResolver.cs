using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class ArgumentResolver
    {
        public Dictionary<string, ArgumentValue> CoerceArgumentValues(
            ObjectType objectType,
            FieldSelection fieldSelection,
            VariableCollection variables)
        {
            Dictionary<string, ArgumentValue> coercedArgumentValues =
                new Dictionary<string, ArgumentValue>();

            Dictionary<string, IValueNode> argumentValues =
                fieldSelection.Node.Arguments
                    .Where(t => t.Value != null)
                    .ToDictionary(t => t.Name.Value, t => t.Value);

            foreach (InputField argument in fieldSelection.Field.Arguments.Values)
            {
                string argumentName = argument.Name;
                IInputType argumentType = argument.Type;
                IValueNode defaultValue = argument.DefaultValue;
                object argumentValue = CoerceArgumentValue(
                    argumentName, argumentType, defaultValue,
                    variables, argumentValues);

                if (argumentType is NonNullType && argumentValue == null)
                {
                    throw new QueryException(new ArgumentError(
                        $"The argument type of '{argumentName}' is a non-null type.",
                        argumentName, fieldSelection.Node));
                }

                coercedArgumentValues[argumentName] = new ArgumentValue(
                    argumentType, argumentType.NativeType, argumentValue);
            }

            return coercedArgumentValues;
        }

        private object CoerceArgumentValue(
            string argumentName,
            IInputType argumentType,
            IValueNode defaultValue,
            VariableCollection variables,
            Dictionary<string, IValueNode> argumentValues)
        {
            if (argumentValues.TryGetValue(argumentName,
                    out IValueNode literal))
            {
                if (literal is VariableNode variable)
                {
                    if (variables.TryGetVariable(
                        variable.Name.Value, out object value))
                    {
                        return value;
                    }
                    return ParseLiteral(argumentType, defaultValue);
                }
                return ParseLiteral(argumentType, literal);
            }
            return ParseLiteral(argumentType, defaultValue);
        }

        private object ParseLiteral(IInputType argumentType, IValueNode value)
        {
            IInputType type = (argumentType is NonNullType)
                ? (IInputType)argumentType.InnerType()
                : argumentType;
            return type.ParseLiteral(value);
        }
    }
}
