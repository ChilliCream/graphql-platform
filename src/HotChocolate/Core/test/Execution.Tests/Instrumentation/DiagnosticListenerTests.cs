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
            IRequestExecutor executor =  await CreateExecutorAsync(c => c
                .AddDiagnosticEventListener(sp => listener)
                .AddStarWarsTypes()
                .Services
                .AddStarWarsRepositories());

            // act
            await executor.ExecuteAsync("{ hero { name } }");

            // assert
            Assert.Collection(listener.Results,
                r => Assert.IsType<Droid>(r),
                r => Assert.IsType<string>(r));
        }


        private class TestListener : DiagnosticEventListener
        {
            public List<object> Results { get; } = new List<object>();

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