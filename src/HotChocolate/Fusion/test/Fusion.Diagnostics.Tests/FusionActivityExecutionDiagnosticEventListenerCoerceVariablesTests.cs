using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Fusion.Diagnostics.Listeners;
using HotChocolate.Fusion.Execution.Caching;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using FusionOperationDocumentNormalizer = HotChocolate.Fusion.Execution.Pipeline.OperationDocumentNormalizer;

namespace HotChocolate.Fusion.Diagnostics;

/// <summary>
/// Pins the invariant that <see cref="FusionActivityExecutionDiagnosticEventListener.CoerceVariables"/>
/// only reads the request context, it never resolves or triggers document normalization as a
/// side effect, and it never throws, regardless of what state the request happens to be in when
/// the event fires.
/// </summary>
[Collection("Instrumentation")]
public sealed class FusionActivityExecutionDiagnosticEventListenerCoerceVariablesTests : FusionTestBase
{
    [Fact]
    public async Task CoerceVariables_Should_Not_Normalize_Or_Throw_When_Slot_Is_Unset()
    {
        // arrange: the normalizer is only ever resolved through GetNormalizedDocument, so a
        // request context whose normalized-document slot was never populated proves the
        // listener never asks for it, independent of whether some pipeline middleware happens
        // to normalize eagerly today. The call into the listener happens synchronously inside
        // the probe middleware itself, because the pooled request context is reset the moment
        // the request completes and can no longer be inspected afterward.
        var normalizeCallCount = 0;
        var invoked = false;
        var wasValidated = false;
        var normalizedDocumentSlotWasUnset = false;
        Exception? coerceVariablesException = null;

        using var server = CreateSourceSchema("a", b => b.AddQueryType<Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .ConfigureSchemaServices(
                    (_, services) => services.AddSingleton<IOperationDocumentNormalizer>(
                        sp => new CountingNormalizer(
                            new FusionOperationDocumentNormalizer(
                                sp.GetRequiredService<FusionSchemaDefinition>(),
                                sp.GetRequiredService<NormalizedDocumentCache>()),
                            () => Interlocked.Increment(ref normalizeCallCount))))
                .UseRequest(
                    (_, _) => context =>
                    {
                        // captured right before OperationVariableCoercionMiddleware would
                        // eagerly normalize, so the normalized-document slot is still unset.
                        // The request is short-circuited here, before the real coercion
                        // middleware runs, so its own eager normalization never reaches the
                        // shared counter and this stays a test of the listener alone.
                        if (!invoked && context.OperationDocumentInfo.Document is not null)
                        {
                            invoked = true;
                            wasValidated = context.OperationDocumentInfo.IsValidated;
                            normalizedDocumentSlotWasUnset = context.OperationDocumentInfo.NormalizedDocument is null;

                            var listenerOptions = new InstrumentationOptions
                            {
                                Scopes = FusionActivityScopes.CoerceVariables
                            };
                            var listener = new FusionActivityExecutionDiagnosticEventListener(
                                new FusionActivityEnricher(listenerOptions),
                                listenerOptions);

                            IDisposable? scope = null;
                            coerceVariablesException =
                                Record.Exception(() => scope = listener.CoerceVariables(context));
                            scope?.Dispose();
                        }

                        context.Result = new OperationResult(
                            ImmutableOrderedDictionary<string, object?>.Empty.Add("probe", true));
                        return ValueTask.CompletedTask;
                    },
                    before: WellKnownRequestMiddleware.OperationVariableCoercionMiddleware,
                    allowMultiple: true));

        var executor = await gateway.Services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New().SetDocument("{ sayHello }").Build();

        // act
        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.True(invoked);
        Assert.True(wasValidated);
        Assert.True(normalizedDocumentSlotWasUnset);
        Assert.Null(coerceVariablesException);
        Assert.Equal(0, normalizeCallCount);
    }

    [Fact]
    public void CoerceVariables_Should_Return_EmptyScope_And_Not_Throw_When_No_Document_Is_Available()
    {
        // arrange: a bare request context, as if the event fired before a document was ever
        // attached to the request.
        var context = new PooledRequestContext();

        var listenerOptions = new InstrumentationOptions { Scopes = FusionActivityScopes.CoerceVariables };
        var listener = new FusionActivityExecutionDiagnosticEventListener(
            new FusionActivityEnricher(listenerOptions),
            listenerOptions);

        // act
        IDisposable? scope = null;
        var exception = Record.Exception(() => scope = listener.CoerceVariables(context));

        // assert
        Assert.Null(exception);
        Assert.Same(FusionExecutionDiagnosticEventListener.EmptyScope, scope);
    }

    private sealed class CountingNormalizer(IOperationDocumentNormalizer inner, Action onNormalize)
        : IOperationDocumentNormalizer
    {
        public DocumentNode NormalizeDocument(RequestContext context)
        {
            onNormalize();
            return inner.NormalizeDocument(context);
        }
    }

    public sealed class Query
    {
        public string SayHello() => "hello";
    }
}
