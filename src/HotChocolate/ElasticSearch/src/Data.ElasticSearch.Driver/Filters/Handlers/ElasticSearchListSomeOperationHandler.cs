using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using static HotChocolate.Data.Filters.DefaultFilterOperations;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// This filter operation handler maps a Some operation field to a <see cref="ISearchOperation"/>
/// </summary>
public class ElasticSearchListSomeOperationHandler
    : FilterFieldHandler<ElasticSearchFilterVisitorContext, ISearchOperation>
{
    /// <summary>
    /// Checks if the field not a filter operations field
    /// </summary>
    /// <param name="context">The current context</param>
    /// <param name="typeDefinition">The definition of the type that declares the field</param>
    /// <param name="fieldDefinition">The definition of the field</param>
    /// <returns>True in case the field can be handled</returns>
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
        => context.Type is IListFilterInputType &&
            fieldDefinition is FilterOperationFieldDefinition { Id: Some };

    /// <inheritdoc />
    public override bool TryHandleEnter(
        ElasticSearchFilterVisitorContext context,
        IFilterField field,
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

        context.RuntimeTypes.Push(field.RuntimeType);
        action = SyntaxVisitor.Continue;
        return true;
    }

    /// <inheritdoc />
    public override bool TryHandleLeave(
        ElasticSearchFilterVisitorContext context,
        IFilterField field,
        ObjectFieldNode node,
        [NotNullWhen(true)] out ISyntaxVisitorAction? action)
    {
        context.RuntimeTypes.Pop();

        action = SyntaxVisitor.Continue;
        return true;
    }
}
