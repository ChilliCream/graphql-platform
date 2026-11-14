using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents the active <c>@stream</c> directive of a field selection.
/// </summary>
/// <param name="InitialCount">
/// The number of list items that are delivered with the parent payload.
/// </param>
/// <param name="Label">
/// The label of the directive, or <c>null</c> if none was specified.
/// </param>
internal readonly record struct StreamUsage(int InitialCount, string? Label)
{
    /// <summary>
    /// Gets the active <c>@stream</c> directive of the selection, resolving variables with
    /// <paramref name="variables"/>. Returns <c>false</c> if the selection has no active directive.
    /// </summary>
    public static bool TryCreate(
        Selection selection,
        IVariableValueCollection variables,
        out StreamUsage usage)
    {
        foreach (var syntaxNode in selection.SyntaxNodes)
        {
            foreach (var directive in syntaxNode.Node.Directives)
            {
                if (directive.Name.Value.Equals(DirectiveNames.Stream.Name, StringComparison.Ordinal))
                {
                    return TryCreate(directive, variables, out usage);
                }
            }
        }

        usage = default;
        return false;
    }

    private static bool TryCreate(
        DirectiveNode directive,
        IVariableValueCollection variables,
        out StreamUsage usage)
    {
        var initialCount = 0;
        string? label = null;

        foreach (var argument in directive.Arguments)
        {
            var name = argument.Name.Value;

            if (name.Equals(DirectiveNames.Stream.Arguments.If, StringComparison.Ordinal))
            {
                if (!DirectiveIfCondition.IsEnabled(
                    DirectiveNames.Stream.Name,
                    argument.Value,
                    variables,
                    defaultValue: true))
                {
                    usage = default;
                    return false;
                }
            }
            else if (name.Equals(DirectiveNames.Stream.Arguments.InitialCount, StringComparison.Ordinal))
            {
                initialCount = GetInitialCount(argument.Value, variables);
            }
            else if (name.Equals(DirectiveNames.Stream.Arguments.Label, StringComparison.Ordinal))
            {
                label = GetLabel(argument.Value, variables);
            }
        }

        usage = new StreamUsage(initialCount, label);
        return true;
    }

    private static int GetInitialCount(IValueNode value, IVariableValueCollection variables)
    {
        if (value is VariableNode variable)
        {
            return variables.TryGetValue<IntValueNode>(variable.Name.Value, out var variableValue)
                ? variableValue.ToInt32()
                : 0;
        }

        return value is IntValueNode intValue ? intValue.ToInt32() : 0;
    }

    private static string? GetLabel(IValueNode value, IVariableValueCollection variables)
    {
        if (value is VariableNode variable)
        {
            return variables.TryGetValue<StringValueNode>(variable.Name.Value, out var variableValue)
                ? variableValue.Value
                : null;
        }

        return value is StringValueNode stringValue ? stringValue.Value : null;
    }
}
