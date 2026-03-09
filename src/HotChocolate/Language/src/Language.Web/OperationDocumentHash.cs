namespace HotChocolate.Language;

/// <summary>
/// Represents the hash of an operation document.
/// </summary>
public readonly struct OperationDocumentHash
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationDocumentHash"/> struct.
    /// </summary>
    /// <param name="value">
    /// The hash of the operation document.
    /// </param>
    /// <param name="algorithm">
    /// The algorithm used to compute the hash.
    /// </param>
    /// <param name="format">
    /// The format of the hash.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> or <paramref name="algorithm"/> is <c>null</c>.
    /// </exception>
    public OperationDocumentHash(string value, string algorithm, HashFormat format)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(value);
        ArgumentException.ThrowIfNullOrEmpty(algorithm);
#else
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (string.IsNullOrEmpty(algorithm))
        {
            throw new ArgumentNullException(nameof(algorithm));
        }
#endif

        Value = value;
        AlgorithmName = algorithm;
        Format = format;
    }

    /// <summary>
    /// Gets an empty operation document hash.
    /// </summary>
    public static OperationDocumentHash Empty => default;

    /// <summary>
    /// Gets the hash of the operation document.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the algorithm used to compute the hash.
    /// </summary>
    public string AlgorithmName { get; }

    /// <summary>
    /// Gets the format of the hash.
    /// </summary>
    public HashFormat Format { get; }

    /// <summary>
    /// Gets a value indicating whether the hash is empty.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(Value);

    /// <summary>
    /// Returns a string representation of the operation document hash.
    /// </summary>
    public override string ToString()
        => IsEmpty ? "(empty)" : $"{AlgorithmName}:{Value} ({Format})";
}
