using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Sorting
{
    public interface ISortFieldHandler<in TContext>
        : ISortFieldHandler
        where TContext : ISortVisitorContext
    {
        bool TryHandleEnter(
            TContext context,
            ISortField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action);

        bool TryHandleLeave(
            TContext context,
            ISortField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action);
    }
}
