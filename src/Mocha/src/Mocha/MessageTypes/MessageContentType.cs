using System.Diagnostics.CodeAnalysis;

namespace Mocha;

/// <summary>
/// Represents a MIME content type used for message serialization, such as JSON, XML, or Protobuf.
/// </summary>
/// <param name="Value">The MIME type string (e.g., "application/json").</param>
public sealed record MessageContentType(string Value)
{
    /// <summary>
    /// The JSON content type ("application/json").
    /// </summary>
    public static readonly MessageContentType Json = new("application/json");

    /// <summary>
    /// The XML content type ("application/xml").
    /// </summary>
    public static readonly MessageContentType Xml = new("application/xml");

    /// <summary>
    /// The Protocol Buffers content type ("application/protobuf").
    /// </summary>
    public static readonly MessageContentType Protobuf = new("application/protobuf");

    /// <summary>
    /// Parses a MIME type string into a <see cref="MessageContentType"/>, returning a well-known instance for standard types.
    /// </summary>
    /// <param name="value">The MIME type string to parse, or <c>null</c>.</param>
    /// <returns>The parsed content type, or <c>null</c> if the input is <c>null</c> or empty.</returns>
    [return: NotNullIfNotNull("value")]
    public static MessageContentType? Parse(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value switch
        {
            "application/json" => Json,
            "application/xml" => Xml,
            "application/protobuf" => Protobuf,
            _ => new MessageContentType(value)
        };
    }
}
