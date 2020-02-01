using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DiagnosticAdapter;
using Xunit;

namespace HotChocolate.Execution
{
    public class DiagnosticTests
    {
        [Fact]
        public async Task ResolverEvents()
        {
            // arrange
            var observer = new TestDiagnosticObserver();
            ISchema schema = CreateSchema();

            // act
            await schema.MakeExecutable(b => b
                .UseDefaultPipeline(new QueryExecutionOptions
                {
                    TracingPreference = TracingPreference.Always
                })
                .AddDiagnosticObserver(observer))
                    .ExecuteAsync("{ foo }");

            // assert
            Assert.True(observer.ResolveFieldStart);
            Assert.True(observer.ResolveFieldStop);
            Assert.Equal("foo", observer.FieldSelection.Name.Value);
            Assert.InRange(observer.Duration,
                TimeSpan.FromMilliseconds(50),
                TimeSpan.FromMilliseconds(2000));
        }

        [Fact]
        public async Task QueryEvents()
        {
            // arrange
            var observer = new TestDiagnosticObserver();
            ISchema schema = CreateSchema();

            // act
            await schema.MakeExecutable(b => b
                .UseDefaultPipeline(new QueryExecutionOptions
                {
                    TracingPreference = TracingPreference.Always
                })
                .AddDiagnosticObserver(observer))
                    .ExecuteAsync("{ foo }");

            // assert
            Assert.True(observer.QueryStart);
            Assert.True(observer.QueryStop);
        }

        private ISchema CreateSchema()
        {
            return Schema.Create(
                "type Query { foo: String }",
                c => c.BindResolver(() =>
                {
                    Thread.Sleep(50);
                    return "bar";
                }).To("Query", "foo"));
        }

        private class TestDiagnosticObserver
            : IDiagnosticObserver
        {
            public bool ResolveFieldStart { get; private set; }

            public bool ResolveFieldStop { get; private set; }

            public TimeSpan Duration { get; private set; }

            public FieldNode FieldSelection { get; private set; }

            public bool QueryStart { get; private set; }

            public bool QueryStop { get; private set; }

            [DiagnosticName("HotChocolate.Execution.Resolver")]
            public virtual void OnResolvField() { }

            [DiagnosticName("HotChocolate.Execution.Resolver.Start")]
            public virtual void OnResolveFieldStart()
            {
                ResolveFieldStart = true;
            }

            [DiagnosticName("HotChocolate.Execution.Resolver.Stop")]
            public virtual void OnResolveFieldStop(IResolverContext context)
            {
                ResolveFieldStop = true;
                FieldSelection = context.FieldSelection;
                Duration = Activity.Current.Duration;
            }

            [DiagnosticName("HotChocolate.Execution.Query")]
            public virtual void OnQuery() { }

            [DiagnosticName("HotChocolate.Execution.Query.Start")]
            public virtual void OnQueryStart()
            {
                QueryStart = true;
            }

            [DiagnosticName("HotChocolate.Execution.Query.Stop")]
            public virtual void OnQueryStop()
            {
                QueryStop = true;
            }
        }
    }
}
