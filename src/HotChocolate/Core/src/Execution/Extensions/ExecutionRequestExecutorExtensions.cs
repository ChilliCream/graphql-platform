using System.Diagnostics.CodeAnalysis;
using static HotChocolate.Execution.Properties.Resources;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

public static class ExecutionRequestExecutorExtensions
{
    public static Task<IExecutionResult> ExecuteAsync(
        this IRequestExecutor executor,
        IOperationRequest request)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(request);

        return executor.ExecuteAsync(
            request,
            CancellationToken.None);
    }

    public static Task<IExecutionResult> ExecuteAsync(
        this IRequestExecutor executor,
        [StringSyntax("graphql")] string query)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrEmpty(query, nameof(query));

        return executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(query).Build(),
            CancellationToken.None);
    }

    public static Task<IExecutionResult> ExecuteAsync(
        this IRequestExecutor executor,
        [StringSyntax("graphql")] string query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrEmpty(query, nameof(query));

        return executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(query).Build(),
            cancellationToken);
    }

    public static Task<IExecutionResult> ExecuteAsync(
        this IRequestExecutor executor,
        [StringSyntax("graphql")] string query,
        Dictionary<string, object?> variableValues)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrEmpty(query, nameof(query));
        ArgumentNullException.ThrowIfNull(variableValues);

        return executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(variableValues)
                .Build(),
            CancellationToken.None);
    }

    public static Task<IExecutionResult> ExecuteAsync(
        this IRequestExecutor executor,
        [StringSyntax("graphql")] string query,
        IReadOnlyDictionary<string, object?> variableValues,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrEmpty(query, nameof(query));
        ArgumentNullException.ThrowIfNull(variableValues);

        return executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(variableValues)
                .Build(),
            cancellationToken);
    }

    public static IExecutionResult Execute(
        this IRequestExecutor executor,
        IOperationRequest request)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(request);

        return Task.Factory.StartNew(
            () => ExecuteAsync(executor, request))
            .Unwrap()
            .GetAwaiter()
            .GetResult();
    }

    public static IExecutionResult Execute(
        this IRequestExecutor executor,
        [StringSyntax("graphql")] string query)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrEmpty(query, nameof(query));

        return executor.Execute(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .Build());
    }

    public static IExecutionResult Execute(
        this IRequestExecutor executor,
        [StringSyntax("graphql")] string query,
        IReadOnlyDictionary<string, object?> variableValues)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrEmpty(query, nameof(query));
        ArgumentNullException.ThrowIfNull(variableValues);

        return executor.Execute(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(variableValues)
                .Build());
    }

    public static Task<IExecutionResult> ExecuteAsync(
        this IRequestExecutor executor,
        Action<OperationRequestBuilder> buildRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(buildRequest);

        var builder = new OperationRequestBuilder();
        buildRequest(builder);
        return executor.ExecuteAsync(
            builder.Build(),
            cancellationToken);
    }

    public static Task<IExecutionResult> ExecuteAsync(
        this IRequestExecutor executor,
        Action<OperationRequestBuilder> buildRequest)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(buildRequest);

        return executor.ExecuteAsync(
            buildRequest,
            CancellationToken.None);
    }
}
