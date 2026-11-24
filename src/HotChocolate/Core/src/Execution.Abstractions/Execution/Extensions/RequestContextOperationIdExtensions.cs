using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Pipeline;
using static HotChocolate.Language.GraphQLCharacters;

namespace HotChocolate.Execution;

/// <summary>
/// Provides the operation id accessor for <see cref="RequestContext"/>.
/// </summary>
public static class RequestContextOperationIdExtensions
{
    // The '.' separator between the operation document id and the operation name.
    private const int OperationIdSeparatorLength = 1;

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

        operationId = context.OperationDocumentInfo.OperationId;
        return operationId is not null;
    }

    /// <summary>
    /// Gets the unique id for the selected operation.
    /// Throws a <see cref="GraphQLException"/> (a request error) when the requested operation name is not
    /// defined, or when the name is omitted in a document with several operations.
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

        var documentInfo = context.OperationDocumentInfo;

        if (documentInfo.OperationId is { } operationId)
        {
            return operationId;
        }

        if (documentInfo.Document is null)
        {
            throw ThrowHelper.OperationId_NoDocument();
        }

        if (documentInfo.Id.IsEmpty)
        {
            throw ThrowHelper.OperationId_DocumentIdEmpty();
        }

        if (documentInfo.OperationCount == 0)
        {
            var requestedName = context.Request.OperationName;

            throw string.IsNullOrEmpty(requestedName)
                ? ThrowHelper.OperationId_NoOperationFound(documentInfo.Document)
                : ThrowHelper.OperationId_OperationNotFound(documentInfo.Document, requestedName);
        }

        if (documentInfo.OperationCount == 1)
        {
            var requestedName = context.Request.OperationName;

            if (!string.IsNullOrEmpty(requestedName)
                && !documentInfo.Document.TryGetOperationDefinition(requestedName, out _))
            {
                throw ThrowHelper.OperationId_OperationNotFound(documentInfo.Document, requestedName);
            }

            operationId = documentInfo.Id.Value;
        }
        else
        {
            var idValue = documentInfo.Id.Value;
            var operationName = context.Request.OperationName;

            if (string.IsNullOrEmpty(operationName))
            {
                throw ThrowHelper.OperationId_OperationNameRequired(documentInfo.Document);
            }

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

        documentInfo.OperationId = operationId;
        return operationId;
    }
}
