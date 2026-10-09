using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

public class OperationFeatureCollectionTests
{
    private static readonly TimeSpan s_handshakeTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_completionTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task GetOrSetSafe_Should_NotDeadlock_When_FactoryRacesLazySelectionSetCompilation()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var compilationStarted = new ManualResetEventSlim();
        using var factoryStarted = new ManualResetEventSlim();

        var schema = SchemaBuilder.New()
            .AddQueryType(
                d =>
                {
                    d.Name("Query");
                    d.Field("first")
                        .Resolve(new Item())
                        .UseOptimizer(new HandshakeOptimizer(compilationStarted, factoryStarted));
                    d.Field("second").Resolve(new Item());
                })
            .Create();

        var document = Utf8GraphQLParser.Parse("{ first { name } second { name } }");
        var operation = OperationCompiler.Compile("opid", document, schema);
        var first = operation.RootSelectionSet.Selections[0];
        var second = operation.RootSelectionSet.Selections[1];

        // act
        // the optimizer of `first` runs while the operation lock is held and writes its selection
        // feature only after the feature factory of `second` has started to compile a selection set.
        // both sides run on dedicated threads to keep the handshake independent of thread-pool
        // scheduling.
        var compilation = Task.Factory.StartNew(
            () => operation.GetSelectionSet(first),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        var featureFactory = Task.Factory.StartNew(
            () =>
            {
                if (!compilationStarted.Wait(s_handshakeTimeout, cancellationToken))
                {
                    throw new InvalidOperationException("The selection set compilation did not start.");
                }

                return second.Features.GetOrSetSafe(
                    () =>
                    {
                        factoryStarted.Set();
                        return new SelectorFeature(operation.GetSelectionSet(second));
                    });
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        var deadlocked = false;

        try
        {
            await Task.WhenAll(compilation, featureFactory)
                .WaitAsync(s_completionTimeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            deadlocked = true;
        }

        // assert
        Assert.False(deadlocked, "The feature factory and the lazy selection set compilation deadlocked.");

        var feature = await featureFactory;

        Assert.True(second.Features.TryGet<SelectorFeature>(out var stored));
        Assert.Same(feature, stored);
    }

    private sealed class HandshakeOptimizer(
        ManualResetEventSlim compilationStarted,
        ManualResetEventSlim factoryStarted)
        : ISelectionSetOptimizer
    {
        public void OptimizeSelectionSet(SelectionSetOptimizerContext context)
        {
            compilationStarted.Set();

            if (!factoryStarted.Wait(s_handshakeTimeout))
            {
                throw new InvalidOperationException("The feature factory did not start.");
            }

            context.CreateSelectionFeatures(context.Selections[0]).SetSafe(new HandshakeFeature());
        }
    }

    private sealed record SelectorFeature(SelectionSet SelectionSet);

    private sealed class HandshakeFeature;

    public sealed class Item
    {
        public string Name => "item";
    }
}
