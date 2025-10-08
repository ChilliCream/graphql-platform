using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public sealed partial class OperationRewriter
{
    private static readonly FieldNode s_typeNameField =
        new FieldNode(
            null,
            new NameNode(IntrospectionFieldNames.TypeName),
            null,
            [new DirectiveNode("fusion__empty")],
            ImmutableArray<ArgumentNode>.Empty,
            null);

    private List<ISelectionNode>? RewriteSelections(BaseContext context)
    {
        List<ISelectionNode>? selections = null;

        if (context.Selections is not null)
        {
            foreach (var selection in context.Selections)
            {
                switch (selection)
                {
                    case FieldNode fieldNode:
                        if (fieldNode.SelectionSet is null)
                        {
                            selections ??= [];
                            selections.Add(fieldNode);
                        }
                        else
                        {
                            if (context.FieldContexts is null
                                || !context.FieldContexts.TryGetValue(fieldNode, out var fieldContext))
                            {
                                throw new InvalidOperationException("Expected to have a field context.");
                            }

                            var fieldSelections = RewriteSelections(fieldContext);

                            if (fieldSelections is null || fieldSelections.Count == 0)
                            {
                                if (!removeStaticallyExcludedSelections)
                                {
                                    continue;
                                }

                                fieldSelections = [s_typeNameField];
                            }

                            var fieldSelection = fieldNode
                                .WithSelectionSet(new SelectionSetNode(fieldSelections));

                            selections ??= [];
                            selections.Add(fieldSelection);
                        }

                        break;

                    case InlineFragmentNode inlineFragmentNode:
                        if (context.FragmentContexts is null
                            || !context.FragmentContexts.TryGetValue(inlineFragmentNode, out var fragmentContext))
                        {
                            throw new InvalidOperationException("Expected to have a fragment context.");
                        }

                        var fragmentSelections = RewriteSelections(fragmentContext);

                        if (fragmentSelections is null)
                        {
                            continue;
                        }

                        var fragmentSelection = inlineFragmentNode
                            .WithSelectionSet(new SelectionSetNode(fragmentSelections));

                        selections ??= [];
                        selections.Add(fragmentSelection);
                        break;
                }
            }
        }

        if (context.ConditionalContexts is not null)
        {
            foreach (var (conditional, conditionalContext) in context.ConditionalContexts)
            {
                var conditionalSelections = RewriteSelections(conditionalContext);

                if (conditionalSelections is null)
                {
                    continue;
                }

                var conditionalDirectives = conditional.ToDirectives();

                ISelectionNode conditionalSelection = conditionalSelections switch
                {
                    [FieldNode { Directives.Count: 0 } fieldNode] => fieldNode
                        .WithDirectives([..fieldNode.Directives, ..conditionalDirectives]),
                    [InlineFragmentNode { Directives.Count: 0 } inlineFragmentNode] => inlineFragmentNode
                        .WithDirectives([..inlineFragmentNode.Directives, ..conditionalDirectives]),
                    _ => new InlineFragmentNode(
                        null,
                        null,
                        conditionalDirectives,
                        new SelectionSetNode(conditionalSelections))
                };

                selections ??= [];
                selections.Add(conditionalSelection);
            }
        }

        return selections;
    }

    private static IReadOnlyList<DirectiveNode> RewriteDirectives(IReadOnlyList<DirectiveNode> directives)
    {
        if (directives.Count == 0)
        {
            return directives;
        }

        if (directives.Count == 1)
        {
            return ImmutableArray<DirectiveNode>.Empty.Add(RewriteDirective(directives[0]));
        }

        var buffer = new DirectiveNode[directives.Count];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = RewriteDirective(directives[0]);
        }

        return ImmutableArray.Create(buffer);
    }

    private static DirectiveNode RewriteDirective(DirectiveNode directive)
    {
        return new DirectiveNode(directive.Name.Value, RewriteArguments(directive.Arguments));
    }

    private static IReadOnlyList<ArgumentNode> RewriteArguments(IReadOnlyList<ArgumentNode> arguments)
    {
        if (arguments.Count == 0)
        {
            return arguments;
        }

        if (arguments.Count == 1)
        {
            return ImmutableArray<ArgumentNode>.Empty.Add(arguments[0].WithLocation(null));
        }

        var buffer = new ArgumentNode[arguments.Count];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = arguments[i].WithLocation(null);
        }

        return ImmutableArray.Create(buffer);
    }
}
