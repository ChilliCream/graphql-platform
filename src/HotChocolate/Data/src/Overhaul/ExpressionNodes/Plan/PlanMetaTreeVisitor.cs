using System.Collections.Generic;
using System.Diagnostics;

namespace HotChocolate.Data.ExpressionNodes;

public abstract class PlanMetaTreeVisitor<TContext>
{
    public virtual void Visit(PlanMetaTree tree, TContext context)
    {
        Visit(tree.Root, context);
    }

    public virtual void Visit(ExpressionNode node, TContext context)
    {
        if (node.Scope is { } scope)
            VisitScope(scope, context);

        var children = node.Children;
        if (children is not null)
            VisitChildren(children, context);
    }

    public virtual void VisitChildren(List<ExpressionNode> children, TContext context)
    {
        foreach (var child in children)
            VisitChild(child, context);
    }

    public virtual void VisitChild(ExpressionNode child, TContext context)
    {
        Visit(child, context);
    }

    public virtual void VisitScope(Scope scope, TContext context)
    {
        Debug.Assert(scope is not null);
        // The root instance eventually includes the instance as a child.
        Visit(scope.RootInstance, context);
    }
}
