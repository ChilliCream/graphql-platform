using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Filters
{
    public interface IFilterFieldHandler
    {
        bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition);
    }

    public interface IFilterFieldHandler<T, TContext>
        : IFilterFieldHandler
        where TContext : FilterVisitorContext<T>
    {
        bool TryHandleEnter(
            TContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action);

        bool TryHandleLeave(
            TContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action);
    }
}
