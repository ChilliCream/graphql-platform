using HotChocolate.Execution.Relay;

namespace HotChocolate.Execution.Options;

/// <summary>
/// Configuration options for GraphQL Global ID serialization behavior.
/// </summary>
public struct NodeIdSerializerOptions
{
    private int _maxIdLength = 1024;
    private int _maxCachedTypeNames = 1024;

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeIdSerializerOptions"/> struct
    /// with default values.
    /// </summary>
    public NodeIdSerializerOptions()
    {
    }

    /// <summary>
    /// Gets or sets the maximum allowed length in characters for a formatted Global ID string.
    /// </summary>
    /// <value>
    /// The maximum length of a Global ID string. Default is 1024 characters.
    /// </value>
    /// <remarks>
    /// <para>
    /// This limit prevents potential denial-of-service attacks through extremely large ID strings
    /// and guards against memory exhaustion during ID parsing operations.
    /// </para>
    /// <para>
    /// Consider increasing this value for applications with complex composite IDs, long type names,
    /// or when using less efficient encoding formats. The actual memory usage depends on the
    /// chosen <see cref="Format"/>:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Base64: ~33% larger than original data</description></item>
    /// <item><description>Hex: 100% larger than original data</description></item>
    /// <item><description>Base36: Variable size, generally larger than Base64</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when set to a value less than 128 characters.
    /// </exception>
    public int MaxIdLength
    {
        readonly get => _maxIdLength;
        set
        {
            if (value < 128)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                     "MaxIdLength must be at least 128.");
            }

            _maxIdLength = value;
        }
    }
    /// <summary>
    /// Gets or sets a value indicating whether to use the new Hot Chocolate Global ID format.
    /// </summary>
    /// <value>
    /// <c>true</c> to use the new format; <c>false</c> to use the legacy format.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// The new format uses a simple delimiter (":") between type name and internal ID:
    /// <c>TypeName:InternalId</c>
    /// </para>
    /// <para>
    /// The legacy format includes additional type indicator bytes for backward compatibility:
    /// <c>TypeName\n[TypeCode]InternalId</c>
    /// </para>
    /// <para>
    /// New applications should use the new simpler format (<c>true</c>) for better performance
    /// and smaller encoded identifier sizes. Set to <c>false</c> only when compatibility with
    /// older Hot Chocolate versions is required.
    /// </para>
    /// </remarks>
    public bool OutputNewIdFormat { get; set; } = true;

    /// <summary>
    /// Gets or sets the encoding format used for Global ID serialization.
    /// </summary>
    /// <value>
    /// A <see cref="NodeIdSerializerFormat"/> value specifying the encoding format.
    /// Default is <see cref="NodeIdSerializerFormat.UrlSafeBase64"/>.
    /// </value>
    /// <remarks>
    /// <para>Available formats:</para>
    /// <list type="table">
    /// <listheader>
    /// <term>Format</term>
    /// <description>Characteristics</description>
    /// </listheader>
    /// <item>
    /// <term><see cref="NodeIdSerializerFormat.Base64"/></term>
    /// <description>Standard Base64 encoding. Compact but contains URL-unsafe characters (+, /, =)</description>
    /// </item>
    /// <item>
    /// <term><see cref="NodeIdSerializerFormat.UrlSafeBase64"/></term>
    /// <description>URL-safe Base64 (- and _ instead of + and /). Recommended for web applications</description>
    /// </item>
    /// <item>
    /// <term><see cref="NodeIdSerializerFormat.UpperHex"/></term>
    /// <description>Uppercase hexadecimal. Human-readable, larger than Base64</description>
    /// </item>
    /// <item>
    /// <term><see cref="NodeIdSerializerFormat.LowerHex"/></term>
    /// <description>Lowercase hexadecimal. Human-readable, larger than Base64</description>
    /// </item>
    /// <item>
    /// <term><see cref="NodeIdSerializerFormat.Base36"/></term>
    /// <description>Mathematical Base36 encoding (0-9, A-Z). Case-insensitive, preserves trailing zeros</description>
    /// </item>
    /// </list>
    /// <para>
    /// Performance considerations:
    /// Base64 formats offer the best size/performance ratio for most applications.
    /// Hex formats are more human-readable but produce larger output.
    /// Base36 provides mathematical properties useful for numeric-heavy IDs.
    /// </para>
    /// </remarks>
    public NodeIdSerializerFormat Format { get; set; } = NodeIdSerializerFormat.UrlSafeBase64;

    /// <summary>
    /// Gets or sets the maximum number of type names to cache for improved performance.
    /// </summary>
    /// <value>
    /// The maximum number of type name byte arrays to cache. Default is 1024.
    /// Minimum value is 128.
    /// </value>
    /// <remarks>
    /// <para>
    /// The serializer caches UTF-8 encoded type names to avoid repeated string-to-byte
    /// conversions during Global ID formatting. This significantly improves performance
    /// for frequently used type names.
    /// </para>
    /// <para>
    /// Memory usage: Each cached entry stores the type name as a UTF-8 byte array.
    /// For typical GraphQL type names (5-20 characters), this uses roughly 10-40 bytes
    /// per entry plus caching overhead.
    /// </para>
    /// <para>
    /// Tuning guidelines:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Small schemas (&lt;50 types): 128-256</description></item>
    /// <item><description>Medium schemas (50-200 types): 512-1024</description></item>
    /// <item><description>Large schemas (&gt;200 types): 1024-2048</description></item>
    /// <item><description>Dynamic schemas: Consider higher values</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when set to a value less than 128 items.
    /// </exception>
    public int MaxCachedTypeNames
    {
        readonly get => _maxCachedTypeNames;
        set
        {
            if (value < 128)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "MaxCachedTypeNames must be at least 128.");
            }

            _maxCachedTypeNames = value;
        }
    }
}
