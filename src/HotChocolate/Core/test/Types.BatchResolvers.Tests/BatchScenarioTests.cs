using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public abstract class BatchScenarioTests : IAsyncLifetime
{
    private readonly List<ServiceProvider> _services = [];

    protected abstract BatchDeclarations Declarations { get; }

    protected BatchProbe Probe { get; } = new();

    protected TestAuthHandler AuthHandler { get; } = new();

    protected Task<IRequestExecutor> CreateExecutorAsync(
        DeclarationStyle style,
        Action<IRequestExecutorBuilder> configure)
        => CreateExecutorAsync(style, configure, TestContext.Current.CancellationToken);

    protected async Task<IRequestExecutor> CreateExecutorAsync(
        DeclarationStyle style,
        Action<IRequestExecutorBuilder> configure,
        CancellationToken cancellationToken)
    {
        var builder = new ServiceCollection()
            .AddSingleton(Probe)
            .AddGraphQLServer(disableDefaultSecurity: true)
            .AddAuthorizationHandler(_ => AuthHandler);

        Declarations[style].Configure(builder);
        configure(builder);

        var services = builder.Services.BuildServiceProvider();
        _services.Add(services);

        return await services.GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync(cancellationToken: cancellationToken);
    }

    protected static Task<IExecutionResult> ExecuteAsync(IRequestExecutor executor, string document)
        => ExecuteAsync(executor, document, TestContext.Current.CancellationToken);

    protected static Task<IExecutionResult> ExecuteAsync(
        IRequestExecutor executor,
        string document,
        CancellationToken cancellationToken)
        => executor.ExecuteAsync(document, cancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

    protected static Task<IExecutionResult> ExecuteAsync(
        IRequestExecutor executor,
        IOperationRequest request)
        => ExecuteAsync(executor, request, TestContext.Current.CancellationToken);

    protected static Task<IExecutionResult> ExecuteAsync(
        IRequestExecutor executor,
        IOperationRequest request,
        CancellationToken cancellationToken)
        => executor.ExecuteAsync(request, cancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

    /// <summary>
    /// Returns the reason a declaration style is not applicable to this family, or
    /// <see langword="null"/> when the style is a real, runnable declaration. Scenario tests must
    /// check this before configuring an executor: a <see cref="Declaration"/> built through
    /// <see cref="Declaration.NotApplicable"/> throws once <c>Configure</c> actually runs.
    /// </summary>
    protected string? GetNotApplicableReason(DeclarationStyle style) => Declarations[style].NotApplicableReason;

    protected Task<SchemaException> ExpectSchemaErrorAsync(
        DeclarationStyle style,
        Action<IRequestExecutorBuilder> configure)
        => ExpectSchemaErrorAsync(style, configure, TestContext.Current.CancellationToken);

    protected Task<SchemaException> ExpectSchemaErrorAsync(
        DeclarationStyle style,
        Action<IRequestExecutorBuilder> configure,
        CancellationToken cancellationToken)
        => Assert.ThrowsAsync<SchemaException>(
            () => CreateExecutorAsync(style, configure, cancellationToken));

    public ValueTask InitializeAsync() => default;

    public async ValueTask DisposeAsync()
    {
        foreach (var services in _services)
        {
            await services.DisposeAsync();
        }
    }
}
