using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Descriptors.Configurations;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Sorting;

/// <summary>
/// Represents a mongodb handler that can be bound to a <see cref="SortField"/>. The handler is
/// executed during the visitation of an input object.
/// </summary>
public abstract class MongoDbSortOperationHandlerBase(
    int operation,
    SortDirection sortDirection)
    : SortOperationHandler<MongoDbSortVisitorContext, MongoDbSortDefinition>
{
    /// <inheritdoc/>
    public override bool CanHandle(
        ITypeCompletionContext context,
        EnumTypeConfiguration typeDefinition,
        SortEnumValueConfiguration valueConfiguration)
    {
        return valueConfiguration.Operation == operation;
    }

    /// <inheritdoc/>
    public override bool TryHandleEnter(
        MongoDbSortVisitorContext context,
        ISortField field,
        SortEnumValue? sortValue,
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
            new MongoDbDirectionalSortOperation(context.GetPath(), sortDirection));

        action = SyntaxVisitor.Continue;
        return true;
    }
}
