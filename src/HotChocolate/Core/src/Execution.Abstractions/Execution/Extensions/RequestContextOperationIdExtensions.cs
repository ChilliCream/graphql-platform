using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Features;
using static HotChocolate.Language.GraphQLCharacters;

namespace HotChocolate.Execution;

/// <summary>
/// Provides the operation id accessor for <see cref="RequestContext"/>.
/// </summary>
public static class RequestContextOperationIdExtensions
{
    // The '.' separator between the operation document id and the operation name.
    private const int OperationIdSeparatorLength = 1;
    private const string DefaultOperationName = "Default";

    /// <summary>
    /// Tries to get the operation id without computing it.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="operationId">
    /// The operation id, or <c>null</c> when it has not been computed yet.
    /// </param>
    /// <returns>
    /// <c>true</c> if the operation id has already been computed, otherwise <c>false</c>.
    /// </returns>
    public static bool TryGetOperationId(
        this RequestContext context,
        [NotNullWhen(true)] out string? operationId)
    {
        ArgumentNullException.ThrowIfNull(context);

        operationId = context.Features.Get<OperationIdInfo>()?.Value;
        return operationId is not null;
    }

    /// <summary>
    /// Gets the unique id for the selected operation.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <returns>
    /// The operation id.
    /// </returns>
    public static string GetOperationId(this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var operationIdInfo = context.Features.GetOrSet<OperationIdInfo>();

        if (operationIdInfo.Value is { } operationId)
        {
            return operationId;
        }

        var documentInfo = context.OperationDocumentInfo;

        if (documentInfo.Document is null)
        {
            throw ThrowHelper.OperationId_NoDocument();
        }

        if (documentInfo.Id.IsEmpty)
        {
            throw ThrowHelper.OperationId_DocumentIdEmpty();
        }

        if (documentInfo.OperationCount == 1)
        {
            operationId = documentInfo.Id.Value;
        }
        else
        {
            var idValue = documentInfo.Id.Value;
            var operationName = context.Request.OperationName ?? DefaultOperationName;
            var maxLength = idValue.Length + OperationIdSeparatorLength + operationName.Length;

            char[]? rented = null;
            var buffer = maxLength <= StackallocThreshold
                ? stackalloc char[maxLength]
                : rented = ArrayPool<char>.Shared.Rent(maxLength);

            try
            {
                idValue.CopyTo(buffer);
                var length = idValue.Length;
                buffer[length++] = '.';

                operationName.CopyTo(buffer[length..]);
                length += operationName.Length;

                operationId = new string(buffer[..length]);
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<char>.Shared.Return(rented);
                }
            }
        }

        operationIdInfo.Value = operationId;
        return operationId;
    }
}
