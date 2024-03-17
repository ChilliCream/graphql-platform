using System;
using System.Collections.Generic;
#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Threading;
using System.Threading.Tasks;
using static HotChocolate.Execution.Properties.Resources;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

public static class ExecutionRequestExecutorExtensions
{
    public static Task<IExecutionResult> ExecuteAsync(
        this IRequestExecutor executor,
        IOperationRequest request)
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return executor.ExecuteAsync(
            request,
            CancellationToken.None);
    }

    public static Task<IExecutionResult> ExecuteAsync(
        this IRequestExecutor executor,
#if NET7_0_OR_GREATER
        [StringSyntax("graphql")] string query)
#else
        string query)
#endif
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException(
                ExecutionRequestExecutorExtensions_ExecuteAsync_QueryCannotBeNullOrEmpty,
                nameof(query));
        }

        return executor.ExecuteAsync(
            OperationRequestBuilder.Create().SetDocument(query).Build(),
            CancellationToken.None);
    }

    public static Task<IExecutionResult> ExecuteAsync(
        this IRequestExecutor executor,
#if NET7_0_OR_GREATER
        [StringSyntax("graphql")] string query,
#else
        string query,
#endif
        CancellationToken cancellationToken)
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException(
                ExecutionRequestExecutorExtensions_ExecuteAsync_QueryCannotBeNullOrEmpty,
                nameof(query));
        }

        return executor.ExecuteAsync(
            OperationRequestBuilder.Create().SetDocument(query).Build(),
            cancellationToken);
    }

    public static Task<IExecutionResult> ExecuteAsync(
        this IRequestExecutor executor,
#if NET7_0_OR_GREATER
        [StringSyntax("graphql")] string query,
#else
        string query,
#endif
        Dictionary<string, object?> variableValues)
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException(
                ExecutionRequestExecutorExtensions_ExecuteAsync_QueryCannotBeNullOrEmpty,
                nameof(query));
        }

        if (variableValues is null)
        {
            throw new ArgumentNullException(nameof(variableValues));
        }

        return executor.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(query)
                .SetVariableValues(variableValues)
                .Build(),
            CancellationToken.None);
    }

    public static Task<IExecutionResult> ExecuteAsync(
        this IRequestExecutor executor,
#if NET7_0_OR_GREATER
        [StringSyntax("graphql")] string query,
#else
        string query,
#endif
        IReadOnlyDictionary<string, object?> variableValues,
        CancellationToken cancellationToken)
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException(
                ExecutionRequestExecutorExtensions_ExecuteAsync_QueryCannotBeNullOrEmpty,
                nameof(query));
        }

        if (variableValues is null)
        {
            throw new ArgumentNullException(nameof(variableValues));
        }

        return executor.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(query)
                .SetVariableValues(variableValues)
                .Build(),
            cancellationToken);
    }

    public static IExecutionResult Execute(
        this IRequestExecutor executor,
        IOperationRequest request)
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return Task.Factory.StartNew(
            () => ExecuteAsync(executor, request))
            .Unwrap()
            .GetAwaiter()
            .GetResult();
    }

    public static IExecutionResult Execute(
        this IRequestExecutor executor,
#if NET7_0_OR_GREATER
        [StringSyntax("graphql")] string query)
#else
        string query)
#endif
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException(
                ExecutionRequestExecutorExtensions_ExecuteAsync_QueryCannotBeNullOrEmpty,
                nameof(query));
        }

        return executor.Execute(
            OperationRequestBuilder.Create()
                .SetDocument(query)
                .Build());
    }

    public static IExecutionResult Execute(
        this IRequestExecutor executor,
#if NET7_0_OR_GREATER
        [StringSyntax("graphql")] string query,
#else
        string query,
#endif
        IReadOnlyDictionary<string, object?> variableValues)
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException(
                ExecutionRequestExecutorExtensions_ExecuteAsync_QueryCannotBeNullOrEmpty,
                nameof(query));
        }

        if (variableValues is null)
        {
            throw new ArgumentNullException(nameof(variableValues));
        }

        return executor.Execute(
            OperationRequestBuilder.Create()
                .SetDocument(query)
                .SetVariableValues(variableValues)
                .Build());
    }

    public static Task<IExecutionResult> ExecuteAsync(
        this IRequestExecutor executor,
        Action<OperationRequestBuilder> buildRequest,
        CancellationToken cancellationToken)
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        if (buildRequest is null)
        {
            throw new ArgumentNullException(nameof(buildRequest));
        }

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
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        if (buildRequest is null)
        {
            throw new ArgumentNullException(nameof(buildRequest));
        }

        return executor.ExecuteAsync(
            buildRequest,
            CancellationToken.None);
    }
}
