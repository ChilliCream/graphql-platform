using System.Buffers;
using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution;

internal static class OperationPlanContextExtensions
{
    /// <summary>
    /// Builds an error from the given exception and reports it against every
    /// result path that the failed operation would have populated. When there
    /// are no variables the error is placed at the root path. Otherwise the
    /// primary and additional paths from each variable value set are collected
    /// into a rented buffer so the error can be reported against every
    /// affected location in a single call.
    /// </summary>
    public static void AddErrors(
        this OperationPlanContext context,
        Exception exception,
        ImmutableArray<VariableValues> variables,
        ResultSelectionSet resultSelectionSet)
    {
        var error = ErrorBuilder.FromException(exception).Build();

        if (variables.Length == 0)
        {
            context.AddErrors(error, resultSelectionSet, Path.Root);
        }
        else
        {
            var pathBufferLength = 0;

            for (var i = 0; i < variables.Length; i++)
            {
                pathBufferLength += 1 + variables[i].AdditionalPaths.Length;
            }

            var pathBuffer = ArrayPool<CompactPath>.Shared.Rent(pathBufferLength);

            try
            {
                var pathBufferIndex = 0;

                for (var i = 0; i < variables.Length; i++)
                {
                    pathBuffer[pathBufferIndex++] = variables[i].Path;

                    foreach (var additionalPath in variables[i].AdditionalPaths)
                    {
                        pathBuffer[pathBufferIndex++] = additionalPath;
                    }
                }

                context.AddErrors(error, resultSelectionSet, pathBuffer.AsSpan(0, pathBufferLength));
            }
            finally
            {
                pathBuffer.AsSpan(0, pathBufferLength).Clear();
                ArrayPool<CompactPath>.Shared.Return(pathBuffer);
            }
        }
    }
}
