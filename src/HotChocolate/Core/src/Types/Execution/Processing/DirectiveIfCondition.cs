using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

internal static class DirectiveIfCondition
{
    /// <summary>
    /// Evaluates the <c>if</c> argument value of a conditional directive.
    /// A <c>null</c> value or a variable that was not provided yields <paramref name="defaultValue"/>,
    /// an explicit <c>null</c> variable or a value that is not a Boolean raises an error.
    /// </summary>
    public static bool IsEnabled(
        string directiveName,
        IValueNode? value,
        IVariableValueCollection variableValues,
        bool defaultValue)
        => Evaluate(directiveName, value, variableValues, hasDefault: true, defaultValue);

    /// <summary>
    /// Evaluates the <c>if</c> argument value of a conditional directive.
    /// A <c>null</c> value, a variable that was not provided, an explicit <c>null</c> variable
    /// or a value that is not a Boolean raises an error.
    /// </summary>
    public static bool IsEnabled(
        string directiveName,
        IValueNode? value,
        IVariableValueCollection variableValues)
        => Evaluate(directiveName, value, variableValues, hasDefault: false, defaultValue: false);

    private static bool Evaluate(
        string directiveName,
        IValueNode? value,
        IVariableValueCollection variableValues,
        bool hasDefault,
        bool defaultValue)
    {
        switch (value)
        {
            case null:
                return hasDefault
                    ? defaultValue
                    : throw ThrowHelper.DirectiveIfValueMissing(directiveName);

            case BooleanValueNode boolean:
                return boolean.Value;

            case VariableNode variable:
                var variableName = variable.Name.Value;

                if (variableValues.TryGetValue<IValueNode>(variableName, out var variableValue))
                {
                    return variableValue switch
                    {
                        BooleanValueNode flag => flag.Value,
                        NullValueNode => throw ThrowHelper.DirectiveIfVariableIsNull(directiveName, variableName),
                        _ => throw ThrowHelper.DirectiveIfVariableInvalidValue(directiveName, variableName)
                    };
                }

                return hasDefault
                    ? defaultValue
                    : throw ThrowHelper.DirectiveIfVariableNotProvided(directiveName, variableName);

            default:
                throw ThrowHelper.DirectiveIfValueInvalid(directiveName);
        }
    }
}
