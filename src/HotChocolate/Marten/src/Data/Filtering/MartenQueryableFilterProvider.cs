using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using Marten.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Marten.Filtering;

/// <summary>
/// The MartenDB Filter Provider.
/// </summary>
public class MartenQueryableFilterProvider : QueryableFilterProvider
{
    /// <summary>
    /// Initializes a new instance of <see cref="MartenQueryableFilterProvider"/>.
    /// </summary>
    /// <param name="configure">
    /// A delegate to configure this provider.
    /// </param>
    public MartenQueryableFilterProvider(
        Action<IFilterProviderDescriptor<QueryableFilterContext>> configure)
        : base(configure)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MartenQueryableFilterProvider"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public MartenQueryableFilterProvider()
    {
    }

    /// <inheritdoc cref="FilterProvider{TContext}"/>
    protected override void Configure(IFilterProviderDescriptor<QueryableFilterContext> descriptor)
    {
        descriptor.AddFieldHandler(MartenQueryableComparableInHandler.Create);
        descriptor.AddFieldHandler(MartenQueryableComparableNotInHandler.Create);
        descriptor.AddFieldHandler(MartenQueryableEnumInHandler.Create);
        descriptor.AddFieldHandler(MartenQueryableEnumNotInHandler.Create);
        descriptor.AddFieldHandler(MartenQueryableStringInHandler.Create);
        descriptor.AddFieldHandler(MartenQueryableStringNotInHandler.Create);
        descriptor.AddDefaultFieldHandlers();
    }

    protected override bool IsInMemoryQuery<TEntityType>(object? input)
        => base.IsInMemoryQuery<TEntityType>(input)
            && input is not IMartenQueryable<TEntityType>;
}
