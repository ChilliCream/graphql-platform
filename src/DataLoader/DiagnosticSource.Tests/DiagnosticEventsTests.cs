using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ChilliCream.Testing;
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
                var batchOptions = new DataLoaderOptions<string>
                {
                    AutoDispatching = true,
                    Batching = true,
                    BatchRequestDelay = TimeSpan.FromMilliseconds(150),
                    Caching = true
                };
                var batchLoader = new DataLoader<string, string>(
                    batchOptions,
                    FetchDataAsync);
                var batchErrorLoader = new DataLoader<string, string>(
                    batchOptions,
                    (keys, canncellationToken) =>
                        throw new Exception("BatchError: Foo"));
                var singleOptions = new DataLoaderOptions<string>
                {
                    AutoDispatching = true,
                    Batching = false,
                    Caching = true
                };
                var singleLoader = new DataLoader<string, string>(
                    singleOptions,
                    FetchDataAsync);

                // act
                await Catch(() => batchLoader.LoadAsync("Foo"))
                    .ConfigureAwait(false);
                await Task.Delay(400).ConfigureAwait(false);
                await Catch(() => batchLoader.LoadAsync("Foo", "Bar"))
                    .ConfigureAwait(false);
                await Task.Delay(400).ConfigureAwait(false);
                await Catch(() => batchLoader.LoadAsync("Bar", "Baz"))
                    .ConfigureAwait(false);
                await Task.Delay(400).ConfigureAwait(false);
                await Catch(() => batchLoader.LoadAsync("Qux"))
                    .ConfigureAwait(false);
                await Task.Delay(400).ConfigureAwait(false);
                await Catch(() => batchErrorLoader.LoadAsync("Foo"))
                    .ConfigureAwait(false);
                await Task.Delay(400).ConfigureAwait(false);
                await Catch(() => singleLoader.LoadAsync("Foo"))
                    .ConfigureAwait(false);
                await Task.Delay(400).ConfigureAwait(false);
                await Catch(() => singleLoader.LoadAsync("Foo", "Bar"))
                    .ConfigureAwait(false);
                await Task.Delay(400).ConfigureAwait(false);
                await Catch(() => singleLoader.LoadAsync("Bar", "Baz"))
                    .ConfigureAwait(false);
                await Task.Delay(400).ConfigureAwait(false);
                await Catch(() => singleLoader.LoadAsync("Qux"))
                    .ConfigureAwait(false);

                // assert
                listener.Snapshot();
            }
        }

        private async Task<IReadOnlyList<Result<string>>> FetchDataAsync(
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

            return await Task.FromResult(results).ConfigureAwait(false);
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
