using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Sorting;

public class ElasticSearchSortProvider : SortProvider<ElasticSearchSortVisitorContext>
{
    /// <inheritdoc/>
    public ElasticSearchSortProvider(
        Action<ISortProviderDescriptor<ElasticSearchSortVisitorContext>> configure)
        : base(configure)
    {
    }

    /// <summary>
    /// The visitor thar will traverse a incoming query and execute the sorting handlers
    /// </summary>
    protected virtual SortVisitor<ElasticSearchSortVisitorContext, ElasticSearchSortOperation>
        Visitor
    { get; } = new();

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
            ElasticSearchSortVisitorContext? visitorContext = null;
            IInputField argument = context.Selection.Field.Arguments[_argumentName];
            IValueNode sort = context.ArgumentLiteral<IValueNode>(_argumentName);

            if (argument.Type.ElementType().NamedType() is ISortInputType sortInputType)
            {
                visitorContext = new ElasticSearchSortVisitorContext(sortInputType, client);

                _provider.Visitor.Visit(sort, visitorContext);

                return visitorContext.Operations.ToArray();
            }

            return Array.Empty<ElasticSearchSortOperation>();
        }
    }
}
