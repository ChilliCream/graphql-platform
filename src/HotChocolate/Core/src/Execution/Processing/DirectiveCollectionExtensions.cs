using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using IHasDirectives = HotChocolate.Language.IHasDirectives;

namespace HotChocolate.Execution.Processing;

internal static class DirectiveCollectionExtensions
{
    public static IValueNode? SkipValue(this IReadOnlyList<DirectiveNode> directives)
    {
        var directive = directives.GetSkipDirective();
        return directive is null ? null : GetIfArgumentValue(directive);
    }

    public static IValueNode? IncludeValue(this IReadOnlyList<DirectiveNode> directives)
    {
        var directive = directives.GetIncludeDirective();
        return directive is null ? null : GetIfArgumentValue(directive);
    }

    public static bool IsDeferrable(this InlineFragmentNode fragmentNode) =>
        fragmentNode.Directives.IsDeferrable();

    public static bool IsDeferrable(this FragmentSpreadNode fragmentSpreadNode) =>
        fragmentSpreadNode.Directives.IsDeferrable();

    public static bool IsDeferrable(this IReadOnlyList<DirectiveNode> directives)
    {
        var directive = directives.GetDeferDirective();
        var ifValue = directive?.GetIfArgumentValueOrDefault();

        // a fragment is not deferrable if we do not find a defer directive or
        // if the `if` of the defer directive is a bool literal with a false value.
        return directive is not null && ifValue is not BooleanValueNode { Value: false };
    }

    public static bool IsStreamable(this FieldNode field) =>
        field.Directives.GetStreamDirective() is not null;

    private static IValueNode GetIfArgumentValue(DirectiveNode directive)
    {
        if (directive.Arguments.Count == 1)
        {
            var argument = directive.Arguments[0];
            if (string.Equals(
                argument.Name.Value,
                WellKnownDirectives.IfArgument,
                StringComparison.Ordinal))
            {
                return argument.Value;
            }
        }

        throw ThrowHelper.MissingIfArgument(directive);
    }

    private static DirectiveNode? GetSkipDirective(
        this IReadOnlyList<DirectiveNode> directives) =>
        GetDirective(directives, WellKnownDirectives.Skip);

    private static DirectiveNode? GetIncludeDirective(
        this IReadOnlyList<DirectiveNode> directives) =>
        GetDirective(directives, WellKnownDirectives.Include);

    internal static DirectiveNode? GetDeferDirective(
        this IHasDirectives container) =>
        GetDirective(container.Directives, WellKnownDirectives.Defer);

    internal static DeferDirective? GetDeferDirective(
        this IReadOnlyList<DirectiveNode> directives,
        IVariableValueCollection variables)
    {
        var directiveNode =
            GetDirective(directives, WellKnownDirectives.Defer);

        if (directiveNode is not null)
        {
            var @if = true;
            string? label = null;

            foreach (var argument in directiveNode.Arguments)
            {
                switch (argument.Name.Value)
                {
                    case WellKnownDirectives.IfArgument:
                        @if = argument.Value switch
                        {
                            VariableNode variable
                                => variables.GetVariable<bool>(variable.Name.Value),
                            BooleanValueNode b => b.Value,
                            _ => @if
                        };
                        break;

                    case WellKnownDirectives.LabelArgument:
                        label = argument.Value switch
                        {
                            VariableNode variable
                                => variables.GetVariable<string?>(variable.Name.Value),
                            StringValueNode b => b.Value,
                            _ => label
                        };
                        break;
                }
            }

            return new DeferDirective(@if, label);
        }

        return null;
    }

    internal static StreamDirective? GetStreamDirective(
        this ISelection selection,
        IVariableValueCollection variables) =>
        selection.SyntaxNode.Directives.GetStreamDirective(variables);

    internal static StreamDirective? GetStreamDirective(
        this IReadOnlyList<DirectiveNode> directives,
        IVariableValueCollection variables)
    {
        var directiveNode = GetDirective(directives, WellKnownDirectives.Stream);

        if (directiveNode is not null)
        {
            var @if = true;
            string? label = null;
            var initialCount = 0;

            foreach (var argument in directiveNode.Arguments)
            {
                switch (argument.Name.Value)
                {
                    case WellKnownDirectives.IfArgument:
                        @if = argument.Value switch
                        {
                            VariableNode variable
                                => variables.GetVariable<bool>(variable.Name.Value),
                            BooleanValueNode b => b.Value,
                            _ => @if
                        };
                        break;

                    case WellKnownDirectives.LabelArgument:
                        label = argument.Value switch
                        {
                            VariableNode variable
                                => variables.GetVariable<string?>(variable.Name.Value),
                            StringValueNode b => b.Value,
                            _ => label
                        };
                        break;

                    case WellKnownDirectives.InitialCount:
                        initialCount = argument.Value switch
                        {
                            VariableNode variable
                                => variables.GetVariable<int>(variable.Name.Value),
                            IntValueNode b => b.ToInt32(),
                            _ => initialCount
                        };
                        break;
                }
            }

            return new StreamDirective(@if, initialCount, label);
        }

        return null;
    }

    internal static IValueNode? GetIfArgumentValueOrDefault(this DirectiveNode directive)
    {
        for (var i = 0; i < directive.Arguments.Count; i++)
        {
            var argument = directive.Arguments[i];

            if (argument.Name.Value.EqualsOrdinal(WellKnownDirectives.IfArgument))
            {
                return argument.Value;
            }
        }

        return null;
    }

    internal static DirectiveNode? GetDeferDirective(
        this IReadOnlyList<DirectiveNode> directives) =>
        GetDirective(directives, WellKnownDirectives.Defer);

    internal static DirectiveNode? GetStreamDirective(
        this FieldNode selection) =>
        GetDirective(selection.Directives, WellKnownDirectives.Stream);

    internal static DirectiveNode? GetStreamDirective(
        this IReadOnlyList<DirectiveNode> directives) =>
        GetDirective(directives, WellKnownDirectives.Stream);

    private static DirectiveNode? GetDirective(
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
            if (directive.Name.Value.EqualsOrdinal(name))
            {
                return directive;
            }
        }
        return null;
    }
}
