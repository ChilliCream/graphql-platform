using System;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// This filter operation handler maps a Equals operation field to a <see cref="ISearchOperation"/>
/// </summary>
public class ElasticSearchStringEqualsOperationHandler
    : ElasticSearchOperationHandlerBase
{
    public ElasticSearchStringEqualsOperationHandler(InputParser inputParser) : base(inputParser)
    {
    }

    /// <inheritdoc />
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
        => context.Type is StringOperationFilterInputType &&
            fieldDefinition is FilterOperationFieldDefinition {Id: DefaultFilterOperations.Equals};

    /// <inheritdoc />
    public override ISearchOperation HandleOperation(
        ElasticSearchFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        if (parsedValue is not string val)
        {
            throw ThrowHelper.Filtering_OnlyStringValuesAllowed(field);
        }

        return new MatchOperation(
            context.GetElasticSearchFilterScope().GetPath(),
            ElasticSearchOperationKind.Filter,
            val);
    }
}
