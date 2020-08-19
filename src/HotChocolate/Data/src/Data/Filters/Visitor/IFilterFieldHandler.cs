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

    public interface IFilterFieldHandler<in TContext>
        : IFilterFieldHandler
        where TContext : IFilterVisitorContext
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

    public interface IFilterFieldHandler<in TContext, T>
        : IFilterFieldHandler<TContext>
        where TContext : FilterVisitorContext<T>
    {
    }
}
