using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
            var listener = new TestDiagnosticListener();
            using (DiagnosticListener.AllListeners.Subscribe(
                new DiagnosticObserver(listener)))
            {
                ISchema schema = CreateSchema();

                // act
                await schema.ExecuteAsync("{ foo }");

                // assert
                Assert.True(listener.ResolveFieldStart);
                Assert.True(listener.ResolveFieldStop);
                Assert.Equal("foo", listener.FieldSelection.Name.Value);
                Assert.InRange(listener.Duration,
                    TimeSpan.FromMilliseconds(50),
                    TimeSpan.FromMilliseconds(2000));
            }
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

        private class TestDiagnosticListener
        {
            public bool ResolveFieldStart { get; private set; }

            public bool ResolveFieldStop { get; private set; }

            public TimeSpan Duration { get; private set; }

            public FieldNode FieldSelection { get; private set; }


            [DiagnosticName("HotChocolate.Execution.ResolveField.Start")]
            public virtual void OnResolveFieldStart()
            {
                ResolveFieldStart = true;
            }

            [DiagnosticName("HotChocolate.Execution.ResolveField.Stop")]
            public virtual void OnResolveFieldStop(IResolverContext context)
            {
                ResolveFieldStop = true;
                FieldSelection = context.FieldSelection;
                Duration = Activity.Current.Duration;
            }
        }

        private class DiagnosticObserver
            : IObserver<DiagnosticListener>
        {
            private readonly TestDiagnosticListener _listener;

            public DiagnosticObserver(TestDiagnosticListener listener)
            {
                _listener = listener;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(DiagnosticListener value)
            {
                if (value.Name == "HotChocolate.Execution")
                {
                    value.SubscribeWithAdapter(_listener, s =>
                    {
                        return true;
                    });
                }
            }
        }
    }
}
