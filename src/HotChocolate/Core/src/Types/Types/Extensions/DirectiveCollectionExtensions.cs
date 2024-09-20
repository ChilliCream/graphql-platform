#nullable enable

using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Utilities;
using ThrowHelper = HotChocolate.Utilities.ThrowHelper;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public static class DirectiveCollectionExtensions
{
    public static T SingleOrDefault<T>(this IDirectiveCollection directives)
    {
        foreach (var directive in directives)
        {
            if (typeof(T).IsAssignableFrom(directive.Type.RuntimeType))
            {
                return directive.AsValue<T>();
            }
        }

        return default!;
    }

    internal static IValueNode? SkipValue(this IReadOnlyList<DirectiveNode> directives)
    {
        var directive = directives.GetSkipDirectiveNode();
        return directive is null
            ? null
            : GetIfArgumentValue(directive);
    }

    internal static IValueNode? IncludeValue(this IReadOnlyList<DirectiveNode> directives)
    {
        var directive = directives.GetIncludeDirectiveNode();
        return directive is null
            ? null
            : GetIfArgumentValue(directive);
    }

    internal static bool IsDeferrable(this InlineFragmentNode fragmentNode)
        => fragmentNode.Directives.IsDeferrable();

    internal static bool IsDeferrable(this FragmentSpreadNode fragmentSpreadNode)
        => fragmentSpreadNode.Directives.IsDeferrable();

    internal static bool IsDeferrable(this IReadOnlyList<DirectiveNode> directives)
    {
        var directive = directives.GetDeferDirectiveNode();
        var ifValue = directive?.GetIfArgumentValueOrDefault();

        // a fragment is not deferrable if we do not find a defer directive or
        // if the `if` of the defer directive is a bool literal with a false value.
        return directive is not null && ifValue is not BooleanValueNode { Value: false, };
    }

    internal static bool IsStreamable(this FieldNode field)
    {
        var directive = field.Directives.GetStreamDirectiveNode();
        var ifValue = directive?.GetIfArgumentValueOrDefault();

        // a field is not streamable if we do not find a streamable directive or
        // if the `if` of the streamable directive is a bool literal with a false value.
        return directive is not null && ifValue is not BooleanValueNode { Value: false, };
    }

    internal static bool HasStreamOrDeferDirective(this IReadOnlyList<DirectiveNode> directives)
    {
        if (directives.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];

            if (directive.Name.Value.EqualsOrdinal(WellKnownDirectives.Defer) ||
                directive.Name.Value.EqualsOrdinal(WellKnownDirectives.Stream))
            {
                return true;
            }
        }

        return false;
    }

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

    private static DirectiveNode? GetSkipDirectiveNode(
        this IReadOnlyList<DirectiveNode> directives)
        => GetDirectiveNode(directives, WellKnownDirectives.Skip);

    private static DirectiveNode? GetIncludeDirectiveNode(
        this IReadOnlyList<DirectiveNode> directives)
        => GetDirectiveNode(directives, WellKnownDirectives.Include);

    internal static DeferDirective? GetDeferDirective(
        this IReadOnlyList<DirectiveNode> directives,
        IVariableValueCollection variables)
    {
        var directiveNode = GetDirectiveNode(directives, WellKnownDirectives.Defer);

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
                            _ => @if,
                        };
                        break;

                    case WellKnownDirectives.LabelArgument:
                        label = argument.Value switch
                        {
                            VariableNode variable
                                => variables.GetVariable<string?>(variable.Name.Value),
                            StringValueNode b => b.Value,
                            _ => label,
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
        selection.SyntaxNode.GetStreamDirective(variables);

    internal static StreamDirective? GetStreamDirective(
        this FieldNode fieldNode,
        IVariableValueCollection variables)
    {
        var directiveNode = GetDirectiveNode(fieldNode.Directives, WellKnownDirectives.Stream);

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
                            _ => @if,
                        };
                        break;

                    case WellKnownDirectives.LabelArgument:
                        label = argument.Value switch
                        {
                            VariableNode variable
                                => variables.GetVariable<string?>(variable.Name.Value),
                            StringValueNode b => b.Value,
                            _ => label,
                        };
                        break;

                    case WellKnownDirectives.InitialCount:
                        initialCount = argument.Value switch
                        {
                            VariableNode variable
                                => variables.GetVariable<int>(variable.Name.Value),
                            IntValueNode b => b.ToInt32(),
                            _ => initialCount,
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

    internal static IValueNode? GetLabelArgumentValueOrDefault(this DirectiveNode directive)
    {
        for (var i = 0; i < directive.Arguments.Count; i++)
        {
            var argument = directive.Arguments[i];

            if (argument.Name.Value.EqualsOrdinal(WellKnownDirectives.LabelArgument))
            {
                return argument.Value;
            }
        }

        return null;
    }

    internal static bool StreamDirectiveEquals(
        this DirectiveNode streamA,
        DirectiveNode streamB)
    {
        var argsA = CreateStreamArgs(streamA);
        var argsB = CreateStreamArgs(streamB);

        return SyntaxComparer.BySyntax.Equals(argsA.If, argsB.If) &&
            SyntaxComparer.BySyntax.Equals(argsA.InitialCount, argsB.InitialCount) &&
            SyntaxComparer.BySyntax.Equals(argsA.Label, argsB.Label);
    }

    private static StreamArgs CreateStreamArgs(DirectiveNode directiveNode)
    {
        var args = new StreamArgs();

        for (var i = 0; i < directiveNode.Arguments.Count; i++)
        {
            var argument = directiveNode.Arguments[i];

            switch (argument.Name.Value)
            {
                case WellKnownDirectives.IfArgument:
                    args.If = argument.Value;
                    break;

                case WellKnownDirectives.LabelArgument:
                    args.Label = argument.Value;
                    break;

                case WellKnownDirectives.InitialCount:
                    args.InitialCount = argument.Value;
                    break;
            }
        }

        return args;
    }

    internal static DirectiveNode? GetDeferDirectiveNode(
        this Language.IHasDirectives container) =>
        GetDirectiveNode(container.Directives, WellKnownDirectives.Defer);

    internal static DirectiveNode? GetDeferDirectiveNode(
        this IReadOnlyList<DirectiveNode> directives) =>
        GetDirectiveNode(directives, WellKnownDirectives.Defer);

    internal static DirectiveNode? GetStreamDirectiveNode(
        this FieldNode selection) =>
        GetDirectiveNode(selection.Directives, WellKnownDirectives.Stream);

    internal static DirectiveNode? GetStreamDirectiveNode(
        this IReadOnlyList<DirectiveNode> directives) =>
        GetDirectiveNode(directives, WellKnownDirectives.Stream);

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

            if (directive.Name.Value.EqualsOrdinal(name))
            {
                return directive;
            }
        }
        return null;
    }

    private ref struct StreamArgs
    {
        public IValueNode? If { get; set; }

        public IValueNode? Label { get; set; }

        public IValueNode? InitialCount { get; set; }
    }
}
