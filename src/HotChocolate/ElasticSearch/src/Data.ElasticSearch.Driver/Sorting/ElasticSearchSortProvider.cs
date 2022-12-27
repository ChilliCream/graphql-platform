using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.ElasticSearch.Sorting;

public class ElasticSearchSortProvider : SortProvider<ElasticSearchSortVisitorContext>
{
    /// <inheritdoc/>
    public ElasticSearchSortProvider(
        Action<ISortProviderDescriptor<ElasticSearchSortVisitorContext>> configure)
        : base(configure)
    {
    }

    /// <inheritdoc />
    public override FieldMiddleware CreateExecutor<TEntityType>(string argumentName)
    {
        return next => context => ExecuteAsync(next, context);

        async ValueTask ExecuteAsync(
            FieldDelegate next,
            IMiddlewareContext context)
        {
            context.LocalContextData =
                context.LocalContextData.SetItem(nameof(IElasticSortFactory),
                    new ElasticSortFactory(this, argumentName));
            await next(context).ConfigureAwait(false);
        }
    }

    private class ElasticSortFactory : IElasticSortFactory
    {
        private readonly ElasticSearchSortProvider _provider;
        private readonly string _argumentName;

        public ElasticSortFactory(
            ElasticSearchSortProvider provider,
            string argumentName)
        {
            _provider = provider;
            _argumentName = argumentName;
        }

        /// <inheritdoc />
        public IReadOnlyList<ElasticSearchSortOperation> Create(IResolverContext context, IAbstractElasticClient client)
        {
            throw new NotImplementedException();
        }
    }
}
