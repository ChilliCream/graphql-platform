using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Resolvers;
using HotChocolate.StarWars;
using HotChocolate.StarWars.Models;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Instrumentation
{
    public class DiagnosticListenerTests
    {
        [Fact]
        public async Task Intercept_Resolver_Result_With_Listener()
        {
            // arrange
            var listener = new TestListener();
            IRequestExecutor executor = await CreateExecutorAsync(c => c
               .AddDiagnosticEventListener(_ => listener)
               .AddStarWarsTypes()
               .Services
               .AddStarWarsRepositories());

            // act
            var result = await executor.ExecuteAsync("{ hero { name } }");

            // assert
            Assert.Null(result.Errors);
            Assert.Collection(listener.Results, r => Assert.IsType<Droid>(r));
        }

        [Fact]
        public async Task Intercept_Resolver_Result_With_Multiple_Listener()
        {
            // arrange
            var listenerA = new TestListener();
            var listenerB = new TestListener();
            IRequestExecutor executor = await CreateExecutorAsync(c => c
               .AddDiagnosticEventListener(_ => listenerA)
               .AddDiagnosticEventListener(_ => listenerB)
               .AddStarWarsTypes()
               .Services
               .AddStarWarsRepositories());

            // act
            var result = await executor.ExecuteAsync("{ hero { name } }");

            // assert
            Assert.Null(result.Errors);
            Assert.Collection(listenerA.Results, r => Assert.IsType<Droid>(r));
            Assert.Collection(listenerB.Results, r => Assert.IsType<Droid>(r));
        }


        private class TestListener : DiagnosticEventListener
        {
            public List<object> Results { get; } = new();

            public override bool EnableResolveFieldValue => true;

            public override IActivityScope ResolveFieldValue(IMiddlewareContext context)
            {
                return new ResolverActivityScope(context, Results);
            }

            private class ResolverActivityScope : IActivityScope
            {
                public ResolverActivityScope(IMiddlewareContext context, List<object> results)
                {
                    Context = context;
                    Results = results;
                }

                private IMiddlewareContext Context { get; }

                public List<object> Results { get; }

                public void Dispose()
                {
                    Results.Add(Context.Result);
                }
            }
        }
    }
}
