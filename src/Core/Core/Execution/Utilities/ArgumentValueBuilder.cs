using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Properties;
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
            var coercedArgumentValues = new Dictionary<string, ArgumentValue>();

            var argumentValues = fieldSelection.Selection.Arguments
                .Where(t => t.Value != null)
                .ToDictionary(t => t.Name.Value, t => t.Value);

            foreach (InputField argument in fieldSelection.Field.Arguments)
            {
                coercedArgumentValues[argument.Name] = CreateArgumentValue(
                    argument, argumentValues, variables,
                    message => QueryError.CreateArgumentError(
                        message,
                        path,
                        fieldSelection.Nodes.First(),
                        argument.Name));
            }

            return coercedArgumentValues;
        }

        private static ArgumentValue CreateArgumentValue(
            InputField argument,
            IDictionary<string, IValueNode> argumentValues,
            IVariableCollection variables,
            Func<string, IError> createError)
        {
            object argumentValue;

            try
            {
                argumentValue = CoerceArgumentValue(
                    argument, variables, argumentValues);
            }
            catch (ScalarSerializationException ex)
            {
                throw new QueryException(createError(ex.Message));
            }

            if (argument.Type is NonNullType && argumentValue == null)
            {
                throw new QueryException(createError(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.ArgumentValueBuilder_NonNull,
                        argument.Name,
                        TypeVisualizer.Visualize(argument.Type))));
            }

            InputTypeNonNullCheck.CheckForNullValueViolation(
                argument.Type, argumentValue, createError);

            return new ArgumentValue(argument.Type, argumentValue);
        }

        private static object CoerceArgumentValue(
            InputField argument,
            IVariableCollection variables,
            IDictionary<string, IValueNode> argumentValues)
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
