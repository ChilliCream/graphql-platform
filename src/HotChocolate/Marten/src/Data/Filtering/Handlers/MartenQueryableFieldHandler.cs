using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Marten.Filtering.Handlers;

public class MartenQueryableFieldHandler: QueryableDefaultFieldHandler
{
    public override bool TryHandleLeave(
        QueryableFilterContext context,
        IFilterField field,
        ObjectFieldNode node,
        [NotNullWhen(true)] out ISyntaxVisitorAction? action)
    {
        if (field.RuntimeType is null)
        {
            action = null;
            return false;
        }

        var condition = context.GetLevel().Dequeue();

        context.PopInstance();
        context.RuntimeTypes.Pop();

        context.GetLevel().Enqueue(condition);
        action = SyntaxVisitor.Continue;
        return true;
    }
}
