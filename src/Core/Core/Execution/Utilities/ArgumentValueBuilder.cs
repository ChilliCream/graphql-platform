using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal static class ArgumentValueBuilder
    {
        public static Dictionary<string, ArgumentValue> CoerceArgumentValues(
            this FieldSelection fieldSelection,
            IVariableCollection variables,
            Path path)
        {
            Dictionary<string, ArgumentValue> coercedArgumentValues =
                new Dictionary<string, ArgumentValue>();

            Dictionary<string, IValueNode> argumentValues =
                fieldSelection.Selection.Arguments
                    .Where(t => t.Value != null)
                    .ToDictionary(t => t.Name.Value, t => t.Value);

            foreach (InputField argument in fieldSelection.Field.Arguments)
            {
                coercedArgumentValues[argument.Name] = CreateArgumentValue(
                    fieldSelection, argument, argumentValues,
                    variables, path);
            }

            return coercedArgumentValues;
        }

        private static ArgumentValue CreateArgumentValue(
            FieldSelection fieldSelection,
            InputField argument,
            Dictionary<string, IValueNode> argumentValues,
            IVariableCollection variables,
            Path path)
        {
            object argumentValue = null;

            try
            {
                argumentValue = CoerceArgumentValue(
                    argument, variables, argumentValues);
            }
            catch (ScalarException ex)
            {
                throw new QueryException(QueryError.CreateArgumentError(
                    ex.Message,
                    path,
                    fieldSelection.Nodes.First(),
                    argument.Name));
            }

            if (argument.Type is NonNullType && argumentValue == null)
            {
                throw new QueryException(QueryError.CreateArgumentError(
                    $"The argument type of '{argument.Name}' is a " +
                    "non-null type.",
                    path,
                    fieldSelection.Nodes.First(),
                    argument.Name));
            }

            return new ArgumentValue(argument.Type, argumentValue);
        }

        private static object CoerceArgumentValue(
            InputField argument,
            IVariableCollection variables,
            Dictionary<string, IValueNode> argumentValues)
        {
            if (argumentValues.TryGetValue(argument.Name,
                out IValueNode literal))
            {
                if (literal is VariableNode variable)
                {
                    if (variables.TryGetVariable(
                        variable.Name.Value, out object value))
                    {
                        return value;
                    }
                    return ParseLiteral(argument.Type, argument.DefaultValue);
                }
                return ParseLiteral(argument.Type, literal);
            }
            return ParseLiteral(argument.Type, argument.DefaultValue);
        }

        private static object ParseLiteral(
            IInputType argumentType,
            IValueNode value)
        {
            IInputType type = (argumentType is NonNullType)
                ? (IInputType)argumentType.InnerType()
                : argumentType;
            return type.ParseLiteral(value);
        }
    }
}
