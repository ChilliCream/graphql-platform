using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.ElasticSearch.Sorting.Handlers;

public abstract class ElasticSearchSortOperationHandlerBase : SortOperationHandler<ElasticSearchSortVisitorContext, ElasticSearchSortOperation>
{
    private readonly int _operation;
    private readonly ElasticSearchSortDirection _sortDirection;

    public ElasticSearchSortOperationHandlerBase(int operation, ElasticSearchSortDirection sortDirection)
    {
        _operation = operation;
        _sortDirection = sortDirection;
    }

    public override bool CanHandle(
        ITypeCompletionContext context,
        EnumTypeDefinition typeDefinition,
        SortEnumValueDefinition valueDefinition)
    {
        return valueDefinition.Operation == _operation;
    }

    /// <inheritdoc/>
    public override bool TryHandleEnter(
        ElasticSearchSortVisitorContext context,
        ISortField field,
        ISortEnumValue? sortValue,
        EnumValueNode node,
        [NotNullWhen(true)] out ISyntaxVisitorAction? action)
    {
        if (sortValue is null)
        {
            context.ReportError(
                ErrorHelper.CreateNonNullError(field, node, context));

            action = null!;
            return false;
        }

        context.Operations.Enqueue(
            new ElasticSearchSortOperation(context.GetPath(), _sortDirection));

        action = SyntaxVisitor.Continue;
        return true;
    }
}
