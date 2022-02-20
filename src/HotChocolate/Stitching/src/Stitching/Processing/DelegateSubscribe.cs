using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Execution;
using static HotChocolate.Stitching.Processing.DelegationHelpers;
using static HotChocolate.Stitching.WellKnownContextData;

namespace HotChocolate.Stitching.Processing;

internal sealed class DelegateSubscribe
{
    public static ValueTask<ISourceStream> SubscribeAsync(IResolverContext context)
    {
        DelegateDirective delegateDirective = context.Selection.Field
            .Directives[DirectiveNames.Delegate]
            .First().ToObject<DelegateDirective>();

        IImmutableStack<SelectionPathComponent> path;
        IImmutableStack<SelectionPathComponent> reversePath;

        if (delegateDirective.Path is null)
        {
            path = ImmutableStack<SelectionPathComponent>.Empty;
            reversePath = ImmutableStack<SelectionPathComponent>.Empty;
        }
        else
        {
            path = SelectionPathParser.Parse(delegateDirective.Path);
            reversePath = ImmutableStack.CreateRange(path);
        }

        context.SetScopedValue(PathField, path);
        context.SetScopedValue(ReversePathField, reversePath);
        context.SetScopedValue(SchemaName, delegateDirective.Schema);

        return new ValueTask<ISourceStream>(
            new RemoteSourceStream(
                context,
                delegateDirective.Schema,
                CreateQuery(context, delegateDirective.Schema, path!, reversePath!)));
    }

    private sealed class RemoteSourceStream : ISourceStream
    {
        private readonly IResolverContext _context;
        private readonly NameString _targetSchema;
        private readonly IQueryRequest _request;

        public RemoteSourceStream(
            IResolverContext context,
            NameString targetSchema,
            IQueryRequest request)
        {
            _context = context;
            _targetSchema = targetSchema;
            _request = request;
        }

        public async IAsyncEnumerable<object> ReadEventsAsync()
        {
            await using SubscriptionResult result =
                await ExecuteSubscribeAsync(
                        _context,
                        _request,
                        _targetSchema)
                    .ConfigureAwait(false);

            await foreach (IQueryResult queryResult in result.ReadResultsAsync()
                .WithCancellation(_context.RequestAborted)
                .ConfigureAwait(false))
            {
                yield return queryResult;
            }
        }

        public ValueTask DisposeAsync() => default;
    }

    private static async ValueTask<SubscriptionResult> ExecuteSubscribeAsync(
        IResolverContext context,
        IQueryRequest request,
        string schemaName)
    {
        IExecutionResult result =
            await context.Service<IStitchingContext>().ExecuteRequestAsync(
                schemaName,
                request,
                context.RequestAborted)
                .ConfigureAwait(false);

        if (result is SubscriptionResult subscriptionResult)
        {
            return subscriptionResult;
        }

        if (result is IQueryResult {Data: null, Errors.Count: > 0} errorResult)
        {
            throw new GraphQLException(errorResult.Errors!);
        }

        throw new GraphQLException(
            "Only subscription results are supported in the subscribe resolver.");
    }
}
