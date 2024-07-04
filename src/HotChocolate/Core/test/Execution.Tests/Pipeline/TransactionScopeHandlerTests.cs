using HotChocolate.Execution.Processing;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;

namespace HotChocolate.Execution.Pipeline;

public class TransactionScopeHandlerTests
{
    [Fact]
    public async Task Custom_Transaction_Is_Correctly_Completed_and_Disposed()
    {
        var completed = false;
        var disposed = false;

        void Complete() => completed = true;
        void Dispose() => disposed = true;

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(100))
            .AddTransactionScopeHandler(_ => new MockTransactionScopeHandler(Complete, Dispose))
            .ExecuteRequestAsync("mutation { doNothing }");

        Assert.True(completed, "transaction must be completed");
        Assert.True(disposed, "transaction must be disposed");
    }

    [Fact]
    public async Task Custom_Transaction_Is_Detects_Error_and_Disposes()
    {
        var completed = false;
        var disposed = false;

        void Complete() => completed = true;
        void Dispose() => disposed = true;

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddTransactionScopeHandler(_ => new MockTransactionScopeHandler(Complete, Dispose))
            .ExecuteRequestAsync("mutation { doError }");

        Assert.False(completed, "transaction was not completed due to error");
        Assert.True(disposed, "transaction must be disposed");
    }

    [Fact]
    public async Task DefaultTransactionScopeHandler_Creates_SystemTransactionScope()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddDefaultTransactionScopeHandler()
            .ExecuteRequestAsync("mutation { foundTransactionScope }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task By_Default_There_Is_No_TransactionScope()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .ExecuteRequestAsync("mutation { foundTransactionScope }")
            .MatchSnapshotAsync();
    }

    public class Query
    {
        public string DoNothing() => "Hello";
    }

    public class Mutation
    {
        public string DoNothing() => "Hello";

        public string DoError() => throw new GraphQLException("I am broken!");

        public bool FoundTransactionScope() =>
            System.Transactions.Transaction.Current is not null;
    }

    public class MockTransactionScopeHandler(Action complete, Action dispose) : ITransactionScopeHandler
    {
        public ITransactionScope Create(IRequestContext context)
            => new MockTransactionScope(complete, dispose, context);
    }

    public class MockTransactionScope(Action complete, Action dispose, IRequestContext context) : ITransactionScope
    {
        public void Complete()
        {
            if(context.Result is IOperationResult { Data: not null, Errors: null or { Count: 0, }, })
            {
                complete();
            }
        }

        public void Dispose() => dispose();
    }
}