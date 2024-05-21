using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public class SortVisitorTestBase
{
    private Func<IResolverContext, IEnumerable<TResult>> BuildResolver<TResult>(
        params TResult?[] results)
        where TResult : class
    {
        return ctx => results.AsQueryable()!;
    }

    protected T[] CreateEntity<T>(params T[] entities) => entities;

    protected IRequestExecutor CreateSchema<TEntity, T>(
        TEntity?[] entities,
        SortConvention? convention = null,
        Action<ISchemaBuilder>? configure = null)
        where TEntity : class
        where T : SortInputType<TEntity>
    {
        convention ??= new SortConvention(x => x.AddDefaults().BindRuntimeType<TEntity, T>());

        var resolver = BuildResolver(entities!);

        var builder = SchemaBuilder.New()
            .AddConvention<ISortConvention>(convention)
            .AddSorting()
            .AddQueryType(
                c =>
                {
                    c
                        .Name("Query")
                        .Field("root")
                        .Resolve(resolver)
                        .UseSorting<T>();

                    c
                        .Name("Query")
                        .Field("rootExecutable")
                        .Resolve(ctx => resolver(ctx).AsExecutable())
                        .UseSorting<T>();
                });

        configure?.Invoke(builder);

        var schema = builder.Create();

        return schema.MakeExecutable();
    }
}
