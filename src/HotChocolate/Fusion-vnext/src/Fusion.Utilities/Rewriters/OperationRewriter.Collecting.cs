using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public sealed partial class OperationRewriter
{
     private void CollectSelections(SelectionSetNode selectionSet, BaseContext context)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    CollectField(field, context);
                    break;

                case InlineFragmentNode inlineFragment:
                    CollectInlineFragment(inlineFragment, context);
                    break;

                case FragmentSpreadNode fragmentSpread:
                    CollectFragmentSpread(fragmentSpread, context);
                    break;
            }
        }
    }

    private void CollectField(FieldNode fieldNode, BaseContext context)
    {
        if (removeStaticallyExcludedSelections && IsStaticallySkipped(fieldNode))
        {
            return;
        }

        var (conditional, directives) = DivideDirectives(
            fieldNode,
            Types.DirectiveLocation.Field);

        conditional = RemoveInheritedConditionals(conditional, context);

        if (conditional is not null)
        {
            context = context.GetOrCreateConditionalContext(conditional);
        }

        fieldNode = fieldNode
            .WithArguments(RewriteArguments(fieldNode.Arguments))
            .WithDirectives(directives ?? [])
            .WithLocation(null);

        var fieldName = fieldNode.Name.Value;
        ITypeDefinition fieldType;

        if (fieldName == IntrospectionFieldNames.TypeName)
        {
            fieldType = schema.Types["String"];
        }
        else
        {
            var field = ((IComplexTypeDefinition)context.Type).Fields[fieldName];

            fieldType = field.Type.AsTypeDefinition();
        }

        var fieldContext = context.GetOrCreateFieldContext(fieldNode, fieldType);

        if (fieldContext is not null && fieldNode.SelectionSet is not null)
        {
            CollectSelections(fieldNode.SelectionSet, fieldContext);
        }
    }

    private void CollectInlineFragment(InlineFragmentNode inlineFragment, BaseContext context)
    {
        if (removeStaticallyExcludedSelections && IsStaticallySkipped(inlineFragment))
        {
            return;
        }

        var typeCondition = inlineFragment.TypeCondition is not null
            ? schema.Types[inlineFragment.TypeCondition.Name.Value]
            : context.Type;

        var (conditional, directives) = DivideDirectives(
            inlineFragment,
            Types.DirectiveLocation.InlineFragment);

        CollectFragment(
            inlineFragment.SelectionSet,
            typeCondition,
            conditional,
            directives,
            context);
    }

    private void CollectFragmentSpread(FragmentSpreadNode fragmentSpread, BaseContext context)
    {
        if (removeStaticallyExcludedSelections && IsStaticallySkipped(fragmentSpread))
        {
            return;
        }

        var fragmentDefinition = context.GetFragmentDefinition(fragmentSpread.Name.Value);
        var typeCondition = schema.Types[fragmentDefinition.TypeCondition.Name.Value];

        var (conditional, directives) = DivideDirectives(
            fragmentSpread,
            Types.DirectiveLocation.InlineFragment);

        CollectFragment(
            fragmentDefinition.SelectionSet,
            typeCondition,
            conditional,
            directives,
            context);
    }

    private void CollectFragment(
        SelectionSetNode selectionSet,
        ITypeDefinition typeCondition,
        Conditional? conditional,
        IReadOnlyList<DirectiveNode>? otherDirectives,
        BaseContext context)
    {
        conditional = RemoveInheritedConditionals(conditional, context);

        if (conditional is not null)
        {
            context = context.GetOrCreateConditionalContext(conditional);
        }

        var isTypeRefinement = !typeCondition.IsAssignableFrom(context.Type);

        BaseContext fragmentContext = context;
        if (isTypeRefinement || otherDirectives is not null)
        {
            var inlineFragment = new InlineFragmentNode(
                null,
                isTypeRefinement
                    ? new NamedTypeNode(typeCondition.Name)
                    : null,
                otherDirectives ?? [],
                selectionSet);

            fragmentContext = context.GetOrCreateFragmentContext(inlineFragment, typeCondition);
        }

        CollectSelections(selectionSet, fragmentContext);
    }

    private static Conditional? RemoveInheritedConditionals(Conditional? conditional, BaseContext context)
    {
        if (conditional is not null)
        {
            var current = context;
            do
            {
                if (current is ConditionalContext conditionalContext)
                {
                    var parentConditional = conditionalContext.Conditional;

                    if (conditional.Skip?.Equals(parentConditional.Skip, SyntaxComparison.Syntax) == true)
                    {
                        conditional.Skip = null;
                    }

                    if (conditional.Include?.Equals(parentConditional.Include, SyntaxComparison.Syntax) == true)
                    {
                        conditional.Include = null;
                    }

                    if (conditional.Skip is null && conditional.Include is null)
                    {
                        return null;
                    }
                }

                current = current.Parent;
            } while (current is not null);
        }

        return conditional;
    }

    private (Conditional? Conditional, IReadOnlyList<DirectiveNode>? Directives) DivideDirectives(
        IHasDirectives directiveProvider,
        Types.DirectiveLocation targetLocation)
    {
        if (directiveProvider.Directives.Count == 0)
        {
            return (null, null);
        }

        Conditional? conditional = null;
        List<DirectiveNode>? directives = null;

        foreach (var directive in directiveProvider.Directives)
        {
            if (schema.DirectiveDefinitions.TryGetDirective(directive.Name.Value, out var directiveDefinition)
                && !directiveDefinition.Locations.HasFlag(targetLocation))
            {
                continue;
            }

            var rewrittenDirective = RewriteDirective(directive);

            if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal))
            {
                if (directive.Arguments is [{ Value: BooleanValueNode }])
                {
                    continue;
                }

                conditional ??= new Conditional();
                conditional.Skip = rewrittenDirective;

                continue;
            }

            if (directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal))
            {
                if (directive.Arguments is [{ Value: BooleanValueNode }])
                {
                    continue;
                }

                conditional ??= new Conditional();
                conditional.Include = rewrittenDirective;

                continue;
            }

            if (directive.Name.Value.Equals(DirectiveNames.Defer.Name, StringComparison.Ordinal))
            {
                var ifArgument = directive.Arguments
                    .FirstOrDefault(a => a.Name.Value.Equals("if", StringComparison.Ordinal));

                if (ifArgument?.Value is BooleanValueNode { Value: false })
                {
                    continue;
                }
            }

            directives ??= [];
            directives.Add(rewrittenDirective);
        }

        return (conditional, directives);
    }
}
