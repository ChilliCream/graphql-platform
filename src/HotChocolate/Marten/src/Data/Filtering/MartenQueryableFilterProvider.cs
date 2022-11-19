using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using Marten.Linq;

namespace HotChocolate.Data.Marten.Filtering;

/// <summary>
/// The MartenDB Filter Provider.
/// </summary>
public class MartenQueryableFilterProvider : QueryableFilterProvider
{
    /// <summary>
    /// Initializes a new instance of <see cref="MartenQueryableFilterProvider"/>.
    /// </summary>
    public MartenQueryableFilterProvider()
    {
    }

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

    /// <inheritdoc cref="FilterProvider{TContext}"/>
    protected override void Configure(IFilterProviderDescriptor<QueryableFilterContext> descriptor)
    {
        descriptor.AddFieldHandler<MartenQueryableComparableInHandler>();
        descriptor.AddFieldHandler<MartenQueryableComparableNotInHandler>();
        descriptor.AddFieldHandler<MartenQueryableEnumInHandler>();
        descriptor.AddFieldHandler<MartenQueryableEnumNotInHandler>();
        descriptor.AddFieldHandler<MartenQueryableStringInHandler>();
        descriptor.AddFieldHandler<MartenQueryableStringNotInHandler>();
        descriptor.AddDefaultFieldHandlers();
    }

    protected override bool IsInMemoryQuery<TEntityType>(object? input)
        => base.IsInMemoryQuery<TEntityType>(input) &&
            input is not IMartenQueryable<TEntityType> and not IMartenQueryable;
}
