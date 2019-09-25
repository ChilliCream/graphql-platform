using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public static class MultiplierHelper
    {
        public static bool TryGetMultiplierValue(
            FieldNode field,
            IVariableValueCollection variables,
            string multiplier,
            out int value)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            if (multiplier == null)
            {
                throw new ArgumentNullException(nameof(multiplier));
            }

            return multiplier.IndexOf('.') == -1
                ? TryGetMultiplierFromArgument(
                    field, variables, multiplier, out value)
                : TryGetMultiplierFromObject(
                    field, variables, multiplier, out value);
        }

        private static bool TryGetMultiplierFromArgument(
           FieldNode field,
           IVariableValueCollection variables,
           string argumentName,
           out int value)
        {
            ArgumentNode argument = field.Arguments
                .FirstOrDefault(t => t.Name.Value == argumentName);

            if (argument == null)
            {
                value = default;
                return false;
            }

            return TryParseValue(argument.Value, variables, out value);
        }

        private static bool TryGetMultiplierFromObject(
            FieldNode field,
            IVariableValueCollection variables,
            string multiplierPath,
            out int value)
        {
            var path = new Queue<string>(multiplierPath.Split('.'));
            string name = path.Dequeue();

            ArgumentNode argument = field.Arguments
                .FirstOrDefault(t => t.Name.Value == name);
            if (argument == null)
            {
                value = default;
                return false;
            }

            IValueNode current = argument.Value;

            while (current is ObjectValueNode)
            {
                if (current is ObjectValueNode obj)
                {
                    current = ResolveObjectField(obj, path);
                }
            }

            return TryParseValue(current, variables, out value);
        }

        private static IValueNode ResolveObjectField(
            ObjectValueNode obj,
            Queue<string> path)
        {
            if (path.Any())
            {
                string name = path.Dequeue();
                ObjectFieldNode fieldValue = obj.Fields
                    .FirstOrDefault(t => t.Name.Value == name);
                return fieldValue.Value;
            }
            else
            {
                return null;
            }
        }

        private static bool TryParseValue(
            IValueNode valueNode,
            IVariableValueCollection variables,
            out int value)
        {
            if (valueNode is VariableNode variable)
            {
                return variables.TryGetVariable(
                    variable.Name.Value, out value);
            }
            else if (valueNode is IntValueNode literal)
            {
                return int.TryParse(
                    literal.Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out value);
            }

            value = default;
            return false;
        }

    }
}
