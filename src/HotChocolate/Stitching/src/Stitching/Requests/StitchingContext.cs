using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Fetching;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Properties;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Requests;

public class StitchingContext : IStitchingContext
{
    private readonly Dictionary<string, RemoteRequestExecutor> _executors = new();

    public StitchingContext(IBatchScheduler batchScheduler, IRequestContext requestContext)
    {
        if (batchScheduler is null)
        {
            throw new ArgumentNullException(nameof(batchScheduler));
        }

        if (requestContext is null)
        {
            throw new ArgumentNullException(nameof(requestContext));
        }

        foreach (var executor in requestContext.Schema.GetRemoteExecutors())
        {
            _executors.Add(
                executor.Key,
                new RemoteRequestExecutor(
                    batchScheduler,
                    executor.Value));
        }
    }

    public IRemoteRequestExecutor GetRemoteRequestExecutor(string schemaName)
    {
        schemaName.EnsureGraphQLName(nameof(schemaName));

        if (_executors.TryGetValue(schemaName, out var executor))
        {
            return executor;
        }

        throw new ArgumentException(
            string.Format(
                CultureInfo.InvariantCulture,
                StitchingResources.SchemaName_NotFound,
                schemaName));
    }

    public ISchema GetRemoteSchema(string schemaName) =>
        GetRemoteRequestExecutor(schemaName).Schema;
}

public sealed class StitchingContextEnricher : IRequestContextEnricher
{
    public void Enrich(IRequestContext context)
    {
        var scheduler = context.Services.GetRequiredService<IBatchScheduler>();
        context.ContextData.Add(
            nameof(IStitchingContext),
            new StitchingContext(scheduler, context));
    }
}

public sealed class StitchingContextParameterExpressionBuilder : CustomParameterExpressionBuilder
{
    private readonly Expression<Func<IResolverContext, IStitchingContext>> _expression =
        ctx => GetStitchingContext(ctx);

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(IStitchingContext);

    public override Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Invoke(_expression, context.ResolverContext);

    private static IStitchingContext GetStitchingContext(IResolverContext context)
    {
        if (context.ContextData.TryGetValue(nameof(IStitchingContext), out var value) &&
            value is IStitchingContext httpContext)
        {
            return httpContext;
        }

        throw new MissingStateException("Stitching", nameof(IStitchingContext), StateKind.Global);
    }
}
