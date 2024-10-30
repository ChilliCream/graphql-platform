using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Data.MongoDb.MongoDbContextData;

namespace HotChocolate.Data.MongoDb.Sorting;

/// <inheritdoc />
public class MongoDbSortProvider : SortProvider<MongoDbSortVisitorContext>
{
    /// <inheritdoc/>
    public MongoDbSortProvider()
    {
    }

    /// <inheritdoc/>
    public MongoDbSortProvider(
        Action<ISortProviderDescriptor<MongoDbSortVisitorContext>> configure)
        : base(configure)
    {
    }

    /// <summary>
    /// The visitor thar will traverse a incoming query and execute the sorting handlers
    /// </summary>
    protected virtual SortVisitor<MongoDbSortVisitorContext, MongoDbSortDefinition> Visitor { get; } = new();

    /// <inheritdoc />
    public override IQueryBuilder CreateBuilder<TEntityType>(string argumentName)
        => new MongoDbQueryBuilder(CreateSortDefinition(argumentName));

    private Func<IMiddlewareContext, MongoDbSortDefinition?> CreateSortDefinition(string argumentName)
        => context =>
        {
            // next we get the sort argument.
            var argument = context.Selection.Field.Arguments[argumentName];
            var sort = context.ArgumentLiteral<IValueNode>(argumentName);

            // if no sort is defined we can stop here and yield back control.
            var skipSorting = context.GetLocalStateOrDefault<bool>(SkipSortingKey);

            // ensure sorting is only applied once
            context.SetLocalState(SkipSortingKey, true);

            if (sort.IsNull() || skipSorting)
            {
                return null;
            }

            if (argument.Type is ListType { ElementType: NonNullType nn, } &&
                nn.NamedType() is SortInputType sortInputType)
            {
                var visitorContext = new MongoDbSortVisitorContext(sortInputType);

                Visitor.Visit(sort, visitorContext);

                if (visitorContext.Errors.Count == 0)
                {
                    return visitorContext.TryCreateQuery(out var order) ? order : null;
                }

                throw new GraphQLException(
                    visitorContext.Errors.Select(e => e.WithPath(context.Path)).ToArray());
            }

            return null;
        };

    private sealed class MongoDbQueryBuilder(Func<IMiddlewareContext, MongoDbSortDefinition?> createSortDefinition) : IQueryBuilder
    {
        public void Prepare(IMiddlewareContext context)
        {
            var sortDef = createSortDefinition(context);
            context.SetLocalState(SortDefinitionKey, sortDef);
        }

        public void Apply(IMiddlewareContext context)
        {
            if (context.Result is not IMongoDbExecutable executable)
            {
                return;
            }

            var sortDef = context.GetLocalStateOrDefault<MongoDbSortDefinition>(SortDefinitionKey);

            if (sortDef is null)
            {
                return;
            }

            context.Result = executable.WithSorting(sortDef);
        }
    }
}
