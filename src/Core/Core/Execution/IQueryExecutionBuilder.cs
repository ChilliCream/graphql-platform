using System.Threading.Tasks;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public delegate QueryDelegate QueryMiddleware(QueryDelegate next);

    public delegate Task QueryDelegate(IQueryContext context);

    public interface IQueryExecutionBuilder
    {
        IServiceCollection Services { get; }

        IQueryExecutionBuilder Use(QueryMiddleware middleware);

        IQueryExecutionBuilder UseField(FieldMiddleware middleware);

        IQueryExecutor Build(ISchema schema);

        void Populate(IServiceCollection services);

        void Populate(IServiceCollection services, bool lazyExecutor);
    }
}
