using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace GreenDonut
{
    public class DiagnosticEventsTests
    {
        private readonly Dictionary<string, Result<string>> _results =
            new Dictionary<string, Result<string>>
            {
                { "Foo", "Qux" },
                { "Bar", "Quux" },
                { "Baz", new Exception("Error: Corge") }
            };

        [Fact(DisplayName = "VerifyEvents: Should match snapshot")]
        public async Task VerifyEvents()
        {
            var listener = new TestListener();
            var observer = new TestObserver(listener);

            using (DiagnosticListener.AllListeners.Subscribe(observer))
            {
                // arrange
                var batchScheduler = new ManualBatchScheduler();
                var loader = new DataLoader<string, string>(batchScheduler, FetchDataAsync);
                var errorLoader = new DataLoader<string, string>(
                    batchScheduler,
                    (keys, cancellationToken) =>
                        throw new Exception("BatchError: Foo"));

                // act
                var tasks = new Task[]
                {
                    Catch(() => loader.LoadAsync("Foo")),
                    Catch(() => loader.LoadAsync("Foo", "Bar")),
                    Catch(() => loader.LoadAsync("Bar", "Baz")),
                    Catch(() => loader.LoadAsync("Qux")),
                    Catch(() => errorLoader.LoadAsync("Foo"))
                };
                batchScheduler.Dispatch();
                await Task.WhenAll(tasks).ConfigureAwait(false);

                // assert
                listener.MatchSnapshot();
            }
        }

        private ValueTask<IReadOnlyList<Result<string>>> FetchDataAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            var results = new Result<string>[keys.Count];

            for (var i = 0; i < keys.Count; i++)
            {
                results[i] = _results.TryGetValue(keys[i],
                    out Result<string> result)
                        ? result
                        : Result<string>.Resolve(null);
            }

            return new ValueTask<IReadOnlyList<Result<string>>>(results);
        }

        private async Task Catch(Func<Task> execute)
        {
            try
            {
                await execute().ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }
}
