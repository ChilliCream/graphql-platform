using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

internal static class DirectiveCollectionExtensions
{
    public static IValueNode? SkipValue(this IReadOnlyList<DirectiveNode> directives)
    {
        DirectiveNode? directive = directives.GetSkipDirective();
        return directive is null ? null : GetIfArgumentValue(directive);
    }

    public static IValueNode? IncludeValue(this IReadOnlyList<DirectiveNode> directives)
    {
        DirectiveNode? directive = directives.GetIncludeDirective();
        return directive is null ? null : GetIfArgumentValue(directive);
    }

    public static bool IsDeferrable(this InlineFragmentNode fragmentNode) =>
        fragmentNode.Directives.GetDeferDirective() is not null;

    public static bool IsDeferrable(this FragmentSpreadNode fragmentSpreadNode) =>
        fragmentSpreadNode.Directives.GetDeferDirective() is not null;

    public static bool IsStreamable(this FieldNode field) =>
        field.Directives.GetStreamDirective() is not null;

    private static IValueNode GetIfArgumentValue(DirectiveNode directive)
    {
        if (directive.Arguments.Count == 1)
        {
            ArgumentNode argument = directive.Arguments[0];
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

    internal static DeferDirective? GetDeferDirective(
        this IReadOnlyList<DirectiveNode> directives,
        IVariableValueCollection variables)
    {
        DirectiveNode? directiveNode =
            GetDirective(directives, WellKnownDirectives.Defer);

        if (directiveNode is not null)
        {
            var @if = true;
            string? label = null;

            foreach (ArgumentNode argument in directiveNode.Arguments)
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
        this IReadOnlyList<DirectiveNode> directives,
        IVariableValueCollection variables)
    {
        DirectiveNode? directiveNode =
            GetDirective(directives, WellKnownDirectives.Stream);

        if (directiveNode is not null)
        {
            var @if = true;
            string? label = null;
            var initialCount = 0;

            foreach (ArgumentNode argument in directiveNode.Arguments)
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

    private static DirectiveNode? GetDeferDirective(
        this IReadOnlyList<DirectiveNode> directives) =>
        GetDirective(directives, WellKnownDirectives.Defer);

    private static DirectiveNode? GetStreamDirective(
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
            DirectiveNode directive = directives[i];
            if (directive.Name.Value.EqualsOrdinal(name))
            {
                return directive;
            }
        }
        return null;
    }
}
