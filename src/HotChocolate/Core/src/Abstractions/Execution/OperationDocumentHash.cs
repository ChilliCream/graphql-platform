using HotChocolate.Language;

namespace HotChocolate.Execution;

/// <summary>
/// Represents the hash of an operation document.
/// </summary>
public readonly struct OperationDocumentHash
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationDocumentHash"/> struct.
    /// </summary>
    /// <param name="hash">
    /// The hash of the operation document.
    /// </param>
    /// <param name="algorithm">
    /// The algorithm used to compute the hash.
    /// </param>
    /// <param name="format">
    /// The format of the hash.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="hash"/> or <paramref name="algorithm"/> is <c>null</c>.
    /// </exception>
    public OperationDocumentHash(string hash, string algorithm, HashFormat format)
    {
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
        AlgorithmName = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
        Format = format;
    }

    /// <summary>
    /// Gets the hash of the operation document.
    /// </summary>
    public string Hash { get; }

    /// <summary>
    /// Gets the algorithm used to compute the hash.
    /// </summary>
    public string AlgorithmName { get; }

    /// <summary>
    /// Gets the format of the hash.
    /// </summary>
    public HashFormat Format { get; }
}
