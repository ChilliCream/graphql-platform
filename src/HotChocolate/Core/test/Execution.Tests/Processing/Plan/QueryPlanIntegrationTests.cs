using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Tests;
using Xunit;

#nullable enable

namespace HotChocolate.Execution.Processing.Plan
{
    public class QueryPlanIntegrationTests
    {
        [Fact]
        public async Task DoNotStopExecutingIfReturnTypeIsNullable()
        {
            var mutation = new Mutation();

            await new ServiceCollection()
                .AddSingleton(mutation)
                .AddGraphQLServer()
                .AddMutationType<Mutation>()
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync("mutation { mutation1 mutation2 }")
                .MatchSnapshotAsync();

            Assert.True(mutation.Invoked1);
            Assert.True(mutation.Invoked2);
        }

        [Fact]
        public async Task EnsureThatExecutionIsAbortedIfMutationRetunrTypeIsNotNullable()
        {
            var mutation = new MutationNonNullReturnTypes();

            await new ServiceCollection()
                .AddSingleton(mutation)
                .AddGraphQLServer()
                .AddMutationType<MutationNonNullReturnTypes>()
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync("mutation { mutation1 mutation2 }")
                .MatchSnapshotAsync();

            Assert.True(mutation.Invoked1);
            Assert.False(mutation.Invoked2);
        }

        public class Mutation
        {
            [GraphQLIgnore]
            public bool Invoked1 { get; set; }

            [GraphQLIgnore]
            public bool Invoked2 { get; set; }

            public string? Mutation1()
            {
                Invoked1 = true;
                throw new Exception("Failed.");
            }

            public string? Mutation2()
            {
                Invoked2 = true;
                return "def";
            }
        }

        public class MutationNonNullReturnTypes
        {
            [GraphQLIgnore]
            public bool Invoked1 { get; set; }

            [GraphQLIgnore]
            public bool Invoked2 { get; set; }

            public string Mutation1()
            {
                Invoked1 = true;
                throw new Exception("Failed.");
            }

            public string Mutation2()
            {
                Invoked2 = true;
                return "def";
            }
        }
    }
}
