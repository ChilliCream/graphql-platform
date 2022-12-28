using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.ElasticSearch.Sorting.Handlers;

public class ElasticSearchDefaultSortFieldHandler
    : SortFieldHandler<ElasticSearchSortVisitorContext, ElasticSearchSortOperation>
{
    /// <inheritdoc />
    public override bool CanHandle(ITypeCompletionContext context, ISortInputTypeDefinition typeDefinition,
        ISortFieldDefinition fieldDefinition) => fieldDefinition.Member is not null;


    /// <inheritdoc />
    public override bool TryHandleEnter(
        ElasticSearchSortVisitorContext context,
        ISortField field,
        ObjectFieldNode node,
        [NotNullWhen(true)] out ISyntaxVisitorAction? action)
    {
        if (node.Value.IsNull())
        {
            context.ReportError(ErrorHelper.CreateNonNullError(field, node.Value, context));

            action = SyntaxVisitor.Skip;
            return true;
        }

        if (field.RuntimeType is null)
        {
            action = null;
            return false;
        }

        context.Path.Push(field.Name);
        context.RuntimeTypes.Push(field.RuntimeType);
        action = SyntaxVisitor.Continue;
        return true;
    }

    /// <inheritdoc />
    public override bool TryHandleLeave(
        ElasticSearchSortVisitorContext context,
        ISortField field,
        ObjectFieldNode node,
        [NotNullWhen(true)] out ISyntaxVisitorAction? action)
    {
        context.RuntimeTypes.Pop();
        context.Path.Pop();

        action = SyntaxVisitor.Continue;
        return true;
    }
}
