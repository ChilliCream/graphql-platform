using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Sorting
{
    public interface ISortOperationHandler<in TContext>
        : ISortOperationHandler
        where TContext : ISortVisitorContext
    {
        bool TryHandleEnter(
            TContext context,
            ISortField field,
            ISortEnumValue? enumValue,
            EnumValueNode valueNode,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action);
    }
}
