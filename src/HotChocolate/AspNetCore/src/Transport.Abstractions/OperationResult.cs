using System;
using System.Text.Json;

namespace HotChocolate.Transport;

/// <summary>
/// Represents the result of a GraphQL operation.
/// </summary>
public sealed class OperationResult : IDisposable
{
    private readonly JsonDocument _document;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationResult"/> class with the
    /// specified JSON document and optional data, errors, and extensions.
    /// </summary>
    /// <param name="document">
    /// The <see cref="JsonDocument"/> object representing the JSON result of the
    /// operation.
    /// </param>
    /// <param name="data">
    /// A <see cref="JsonElement"/> object representing the data returned by the operation.
    /// </param>
    /// <param name="errors">
    /// A <see cref="JsonElement"/> object representing any errors that occurred during
    /// the operation.
    /// </param>
    /// <param name="extensions">
    /// A <see cref="JsonElement"/> object representing any extensions returned by the
    /// operation.
    /// </param>
    public OperationResult(
        JsonDocument document,
        JsonElement data = default,
        JsonElement errors = default,
        JsonElement extensions = default)
    {
        _document = document;
        Data = data;
        Errors = errors;
        Extensions = extensions;
    }

    /// <summary>
    /// Gets the <see cref="JsonElement"/> object representing the data returned by
    /// the operation.
    /// </summary>
    public JsonElement Data { get; }

    /// <summary>
    /// Gets the <see cref="JsonElement"/> object representing any errors that occurred
    /// during the operation.
    /// </summary>
    public JsonElement Errors { get; }

    /// <summary>
    /// Gets the <see cref="JsonElement"/> object representing any extensions returned
    /// by the operation.
    /// </summary>
    public JsonElement Extensions { get; }

    /// <summary>
    /// Releases all resources used by the <see cref="OperationResult"/> object.
    /// </summary>
    public void Dispose()
        => _document.Dispose();
}
