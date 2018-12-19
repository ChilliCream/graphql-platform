using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public delegate QueryDelegate QueryMiddleware(QueryDelegate next);

    public delegate Task QueryDelegate(IQueryContext context);

    public interface IQueryExecutionBuilder
    {
        IServiceCollection Services { get; }

        IQueryExecutionBuilder Use(QueryMiddleware middleware);

        IQueryExecuter Build(ISchema schema);
    }
}
