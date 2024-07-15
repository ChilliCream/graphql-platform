using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static System.StringComparer;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationCompiler2
{
    private ArgumentMap? CoerceArgumentValues(
        ObjectField field,
        FieldNode selection,
        string responseName)
    {
        if (field.Arguments.Count == 0)
        {
            return null;
        }

        var arguments = new Dictionary<string, ArgumentValue>(Ordinal);

        for (var i = 0; i < selection.Arguments.Count; i++)
        {
            var argumentValue = selection.Arguments[i];
            if (field.Arguments.TryGetField(
                argumentValue.Name.Value,
                out var argument))
            {
                arguments[argument.Name] =
                    CreateArgumentValue(
                        responseName,
                        argument,
                        argumentValue,
                        argumentValue.Value,
                        false);
            }
        }

        for (var i = 0; i < field.Arguments.Count; i++)
        {
            var argument = field.Arguments[i];
            if (!arguments.ContainsKey(argument.Name))
            {
                arguments[argument.Name] =
                    CreateArgumentValue(
                        responseName,
                        argument,
                        null,
                        argument.DefaultValue ?? NullValueNode.Default,
                        true);
            }
        }

        return new ArgumentMap(arguments);
    }

    private ArgumentValue CreateArgumentValue(
        string responseName,
        IInputField argument,
        ArgumentNode? argumentValue,
        IValueNode value,
        bool isDefaultValue)
    {
        var validationResult =
            ArgumentNonNullValidator.Validate(
                argument,
                value,
                Path.Root.Append(argument.Name));

        if (argumentValue is not null && validationResult.HasErrors)
        {
            return new ArgumentValue(
                argument,
                ErrorHelper.ArgumentNonNullError(
                    argumentValue,
                    responseName,
                    validationResult));
        }

        if (argument.Type.IsLeafType() && CanBeCompiled(value))
        {
            try
            {
                return new ArgumentValue(
                    argument,
                    value.GetValueKind(),
                    true,
                    isDefaultValue,
                    _inputParser.ParseLiteral(value, argument),
                    value);
            }
            catch (SerializationException ex)
            {
                if (argumentValue is not null)
                {
                    return new ArgumentValue(
                        argument,
                        ErrorHelper.ArgumentValueIsInvalid(argumentValue, responseName, ex));
                }

                return new ArgumentValue(
                    argument,
                    ErrorHelper.ArgumentDefaultValueIsInvalid(responseName, ex));
            }
        }

        return new ArgumentValue(
            argument,
            value.GetValueKind(),
            false,
            isDefaultValue,
            null,
            value);
    }

    private static bool CanBeCompiled(IValueNode valueLiteral)
    {
        switch (valueLiteral.Kind)
        {
            case SyntaxKind.Variable:
            case SyntaxKind.ObjectValue:
                return false;

            case SyntaxKind.ListValue:
                var list = (ListValueNode)valueLiteral;
                for (var i = 0; i < list.Items.Count; i++)
                {
                    if (!CanBeCompiled(list.Items[i]))
                    {
                        return false;
                    }
                }
                break;
        }

        return true;
    }
}
