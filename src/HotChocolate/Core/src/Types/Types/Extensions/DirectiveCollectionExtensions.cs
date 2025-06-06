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
    public static T? SingleOrDefault<T>(this DirectiveCollection directives) where T : notnull
    {
        foreach (var directive in directives)
        {
            if (typeof(T).IsAssignableFrom(directive.Type.RuntimeType))
            {
                return directive.ToValue<T>();
            }
        }

        return default;
    }

    internal static IValueNode? SkipValue(this IReadOnlyList<DirectiveNode> directives)
    {
        var directive = directives.GetSkipDirectiveNode();

        if (directive is null)
        {
            return null;
        }

        return directive.GetArgumentValue(DirectiveNames.Skip.Arguments.If, BooleanValueNode.True)
            ?? throw ThrowHelper.MissingIfArgument(directive);
    }

    internal static IValueNode? IncludeValue(this IReadOnlyList<DirectiveNode> directives)
    {
        var directive = directives.GetIncludeDirectiveNode();

        if (directive is null)
        {
            return null;
        }

        return directive.GetArgumentValue(DirectiveNames.Include.Arguments.If, BooleanValueNode.True)
            ?? throw ThrowHelper.MissingIfArgument(directive);
    }

    internal static bool IsDeferrable(this InlineFragmentNode fragmentNode)
        => fragmentNode.Directives.IsDeferrable();

    internal static bool IsDeferrable(this FragmentSpreadNode fragmentSpreadNode)
        => fragmentSpreadNode.Directives.IsDeferrable();

    internal static bool IsDeferrable(this IReadOnlyList<DirectiveNode> directives)
    {
        var directive = directives.GetDeferDirectiveNode();
        var ifValue = directive?.GetArgumentValue(DirectiveNames.Defer.Arguments.If, BooleanValueNode.True);

        // a fragment is not deferrable if we do not find a defer directive or
        // if the `if`-argument of the defer directive is a bool literal with a false value.
        return directive is not null && ifValue is not { Value: false };
    }

    internal static DirectiveNode? GetSkipDirectiveNode(
        this IReadOnlyList<DirectiveNode> directives)
        => GetDirectiveNode(directives, DirectiveNames.Skip.Name);

    internal static DirectiveNode? GetIncludeDirectiveNode(
        this IReadOnlyList<DirectiveNode> directives)
        => GetDirectiveNode(directives, DirectiveNames.Include.Name);

    internal static DeferDirective? GetDeferDirective(
        this IReadOnlyList<DirectiveNode> directives,
        IVariableValueCollection variables)
    {
        var directiveNode = GetDirectiveNode(directives, DirectiveNames.Defer.Name);

        if (directiveNode is not null)
        {
            var @if = true;
            string? label = null;

            foreach (var argument in directiveNode.Arguments)
            {
                switch (argument.Name.Value)
                {
                    case DirectiveNames.Defer.Arguments.If:
                        @if = argument.Value switch
                        {
                            VariableNode variable => !variables.GetBooleanValue(variable.Name.Value) ?? true,
                            BooleanValueNode b => b.Value,
                            _ => true
                        };
                        break;

                    case DirectiveNames.Defer.Arguments.Label:
                        label = argument.Value switch
                        {
                            VariableNode variable => variables.GetStringValue(variable.Name.Value),
                            StringValueNode b => b.Value,
                            _ => null
                        };
                        break;
                }
            }

            return new DeferDirective(@if, label);
        }

        return null;
    }

    private static bool? GetBooleanValue(this IVariableValueCollection variables, string variableName)
        => variables.TryGetValue(variableName, out BooleanValueNode? value) ? value.Value : null;

    private static string? GetStringValue(this IVariableValueCollection variables, string variableName)
        => variables.TryGetValue(variableName, out StringValueNode? value) ? value.Value : null;

    private static int? GetIntValue(this IVariableValueCollection variables, string variableName)
        => variables.TryGetValue(variableName, out IntValueNode? value) ? value.ToInt32() : null;

    internal static StreamDirective? GetStreamDirective(
        this ISelection selection,
        IVariableValueCollection variables) =>
        selection.SyntaxNode.GetStreamDirective(variables);

    internal static StreamDirective? GetStreamDirective(
        this FieldNode fieldNode,
        IVariableValueCollection variables)
    {
        var directiveNode = GetDirectiveNode(fieldNode.Directives, DirectiveNames.Stream.Name);

        if (directiveNode is not null)
        {
            var @if = true;
            string? label = null;
            var initialCount = 0;

            foreach (var argument in directiveNode.Arguments)
            {
                switch (argument.Name.Value)
                {
                    case DirectiveNames.Stream.Arguments.If:
                        @if = argument.Value switch
                        {
                            VariableNode variable => !variables.GetBooleanValue(variable.Name.Value) ?? true,
                            BooleanValueNode b => b.Value,
                            _ => true
                        };
                        break;

                    case DirectiveNames.Stream.Arguments.Label:
                        label = argument.Value switch
                        {
                            VariableNode variable => variables.GetStringValue(variable.Name.Value),
                            StringValueNode b => b.Value,
                            _ => null
                        };
                        break;

                    case DirectiveNames.Stream.Arguments.InitialCount:
                        initialCount = argument.Value switch
                        {
                            VariableNode variable =>  variables.GetIntValue(variable.Name.Value) ?? 0,
                            IntValueNode b => b.ToInt32(),
                            _ => 0
                        };
                        break;
                }
            }

            return new StreamDirective(@if, initialCount, label);
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
                case DirectiveNames.Stream.Arguments.If:
                    args.If = argument.Value;
                    break;

                case DirectiveNames.Stream.Arguments.Label:
                    args.Label = argument.Value;
                    break;

                case DirectiveNames.Stream.Arguments.InitialCount:
                    args.InitialCount = argument.Value;
                    break;
            }
        }

        return args;
    }

    internal static DirectiveNode? GetDeferDirectiveNode(
        this Language.IHasDirectives container) =>
        GetDirectiveNode(container.Directives, DirectiveNames.Defer.Name);

    internal static DirectiveNode? GetDeferDirectiveNode(
        this IReadOnlyList<DirectiveNode> directives) =>
        GetDirectiveNode(directives, DirectiveNames.Defer.Name);

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
