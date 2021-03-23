using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Pipeline
{
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
                .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(100))
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
                .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(100))
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
                .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(100))
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

        public class MockTransactionScopeHandler : ITransactionScopeHandler
        {
            private readonly Action _complete;
            private readonly Action _dispose;

            public MockTransactionScopeHandler(
                Action complete,
                Action dispose)
            {
                _complete = complete;
                _dispose = dispose;
            }

            public ITransactionScope Create(IRequestContext context)
            {
                return new MockTransactionScope(_complete, _dispose, context);
            }
        }

        public class MockTransactionScope : ITransactionScope
        {
            private readonly Action _complete;
            private readonly Action _dispose;
            private readonly IRequestContext _context;

            public MockTransactionScope(
                Action complete,
                Action dispose,
                IRequestContext context)
            {
                _complete = complete;
                _dispose = dispose;
                _context = context;
            }

            public void Complete()
            {
                if(_context.Result is IQueryResult { Data: not null, Errors: null or { Count: 0 } })
                {
                    _complete();
                }
            }

            public void Dispose()
            {
                _dispose();
            }
        }
    }
}
