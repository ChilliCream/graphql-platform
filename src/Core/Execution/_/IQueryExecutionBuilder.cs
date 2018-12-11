using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public interface IQueryExecutionBuilder
    {
        IServiceCollection Services { get; }

        IQueryExecutionBuilder Use(QueryMiddleware middleware);

        IQueryExecuter BuildQueryExecuter(ISchema schema);
    }

    public delegate QueryDelegate QueryMiddleware(QueryDelegate next);

    public delegate Task QueryDelegate(IQueryContext context);

    internal delegate Task ClassQueryDelegate(
        IQueryContext context,
        IServiceProvider services,
        Type middlewareType);
}

