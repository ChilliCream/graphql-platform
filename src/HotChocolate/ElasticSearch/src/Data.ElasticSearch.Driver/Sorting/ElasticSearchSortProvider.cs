using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Data.ElasticSearch.ElasticSearchContextData;

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
    public override IQueryBuilder CreateBuilder<TEntityType>(string argumentName)
        => new ElasticSearchQueryBuilder(CreateSortDefinition(argumentName));

    private Func<IMiddlewareContext, IReadOnlyList<ElasticSearchSortOperation>?> CreateSortDefinition(
        string argumentName)
        => context =>
        {
            var argument = context.Selection.Field.Arguments[argumentName];
            var sort = context.ArgumentLiteral<IValueNode>(argumentName);

            var skipSorting = context.GetLocalStateOrDefault<bool>(SkipSortingKey);
            context.SetLocalState(SkipSortingKey, true);

            if (sort.IsNull() || skipSorting)
            {
                return null;
            }

            if (argument.Type is ListType { ElementType: NonNullType nn }
                && nn.NamedType() is ISortInputType sortInputType)
            {
                var visitorContext = new ElasticSearchSortVisitorContext(sortInputType);
                Visitor.Visit(sort, visitorContext);

                if (visitorContext.Errors.Count == 0)
                {
                    return visitorContext.Operations.ToArray();
                }

                throw new GraphQLException(
                    visitorContext.Errors.Select(e => e.WithPath(context.Path)).ToArray());
            }

            return null;
        };

    private sealed class ElasticSearchQueryBuilder(
        Func<IMiddlewareContext, IReadOnlyList<ElasticSearchSortOperation>?> createSortDefinition)
        : IQueryBuilder
    {
        public void Prepare(IMiddlewareContext context)
        {
            var sortDefinition = createSortDefinition(context);
            context.SetLocalState(SortDefinitionKey, sortDefinition);
        }

        public void Apply(IMiddlewareContext context)
        {
            if (context.Result is not IElasticSearchExecutable executable)
            {
                return;
            }

            var sortDefinition =
                context.GetLocalStateOrDefault<IReadOnlyList<ElasticSearchSortOperation>>(
                    SortDefinitionKey);

            if (sortDefinition is null)
            {
                return;
            }

            context.Result = executable.WithSorting(sortDefinition);
        }
    }
}
