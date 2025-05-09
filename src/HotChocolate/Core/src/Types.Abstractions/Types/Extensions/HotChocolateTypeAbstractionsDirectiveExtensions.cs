#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
using HotChocolate.Language;

namespace HotChocolate.Types;

public static class HotChocolateTypeAbstractionsDirectiveExtensions
{
    public static bool IsStreamable(this FieldNode field)
    {
        ArgumentNullException.ThrowIfNull(field);

        var directive = field.GetStreamDirective();
        var value = directive?.GetArgumentValue(DirectiveNames.Stream.Arguments.If, BooleanValueNode.True);
        return directive is not null && value is not BooleanValueNode { Value: false };
    }

    public static DirectiveNode? GetStreamDirective(
        this FieldNode selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        return GetDirectiveNode(selection.Directives, DirectiveNames.Stream.Name);
    }

    public static IValueNode? GetArgumentValue(this DirectiveNode directive, string name)
    {
        ArgumentNullException.ThrowIfNull(directive);
        ArgumentException.ThrowIfNullOrEmpty(name);

        for (var i = 0; i < directive.Arguments.Count; i++)
        {
            var argument = directive.Arguments[i];

            if (argument.Name.Value.Equals(name, StringComparison.Ordinal))
            {
                return argument.Value;
            }
        }

        return null;
    }

    public static T GetArgumentValue<T>(this DirectiveNode directive, string name, T defaultValue)
        where T : notnull, IValueNode
    {
        ArgumentNullException.ThrowIfNull(directive);
        ArgumentException.ThrowIfNullOrEmpty(name);

        for (var i = 0; i < directive.Arguments.Count; i++)
        {
            var argument = directive.Arguments[i];

            if (argument.Name.Value.Equals(name, StringComparison.Ordinal))
            {
                return argument.Value is T value ? value : defaultValue;
            }
        }

        return defaultValue;
    }

    private static DirectiveNode? GetDirectiveNode(
        this IReadOnlyList<DirectiveNode> directives,
        string name)
    {
        if (directives.Count == 0)
        {
            return null;
        }

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];

            if (directive.Name.Value.Equals(name, StringComparison.Ordinal))
            {
                return directive;
            }
        }

        return null;
    }
}
