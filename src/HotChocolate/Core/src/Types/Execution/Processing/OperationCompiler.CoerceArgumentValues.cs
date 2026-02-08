using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    private ArgumentMap? CoerceArgumentValues(
        ObjectField field,
        FieldNode selection)
    {
        if (field.Arguments.Count == 0)
        {
            return null;
        }

        var arguments = new Dictionary<string, ArgumentValue>(StringComparer.Ordinal);

        for (var i = 0; i < selection.Arguments.Count; i++)
        {
            var argumentValue = selection.Arguments[i];
            if (field.Arguments.TryGetField(
                argumentValue.Name.Value,
                out var argument))
            {
                arguments[argument.Name] = CreateArgumentValue(argument, argumentValue, argumentValue.Value, false);
            }
        }

        for (var i = 0; i < field.Arguments.Count; i++)
        {
            var argument = field.Arguments[i];
            if (!arguments.ContainsKey(argument.Name))
            {
                var value = argument.DefaultValue ?? NullValueNode.Default;
                arguments[argument.Name] = CreateArgumentValue(argument, null, value, true);
            }
        }

        return new ArgumentMap(arguments);
    }

    private ArgumentValue CreateArgumentValue(
        Argument argument,
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
                    _inputValueParser.ParseLiteral(value, argument),
                    value);
            }
            catch (LeafCoercionException ex)
            {
                return new ArgumentValue(argument, ex.Errors[0]);
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
