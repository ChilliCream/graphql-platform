using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using Marten.Linq;

namespace HotChocolate.Data.Marten.Sorting;

/// <summary>
/// The MartenDB Sort Provider.
/// </summary>
public class MartenQueryableSortProvider : QueryableSortProvider
{
    /// <summary>
    /// Initializes a new instance of <see cref="MartenQueryableSortProvider"/>.
    /// </summary>
    public MartenQueryableSortProvider()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MartenQueryableSortProvider"/>.
    /// </summary>
    /// <param name="configure">
    /// A delegate to configure this provider.
    /// </param>
    public MartenQueryableSortProvider(
        Action<ISortProviderDescriptor<QueryableSortContext>> configure)
        : base(configure)
    {
    }

    public override IQueryBuilder CreateBuilder<TEntityType>(string argumentName)
        => new MartenQueryBuilder<TEntityType>(base.CreateBuilder<TEntityType>(argumentName));

    /// <inheritdoc cref="SortProvider{TContext}"/>
    protected override void Configure(ISortProviderDescriptor<QueryableSortContext> descriptor)
        => descriptor.AddDefaultFieldHandlers();

    protected override bool IsInMemoryQuery<TEntityType>(object? input)
        => base.IsInMemoryQuery<TEntityType>(input)
            && input is not IMartenQueryable<TEntityType>;
}
