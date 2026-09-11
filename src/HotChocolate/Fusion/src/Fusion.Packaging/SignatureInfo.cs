using System.Security.Cryptography.X509Certificates;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Contains information about the digital signature of a Fusion Archive,
/// including the signing certificate and verification status.
/// </summary>
public record SignatureInfo
{
    /// <summary>
    /// Gets the time when the signature was created, taken from the CMS signing-time attribute.
    /// This value is <c>null</c> when the signature carries no signing-time attribute.
    /// </summary>
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>
    /// Gets the digest algorithm used for signature creation as a lowercase name (e.g., "sha256").
    /// </summary>
    public required string Algorithm { get; init; }

    /// <summary>
    /// Gets or sets the X.509 certificate that was used to create the signature.
    /// This certificate is embedded in the signature and contains the public key
    /// used for signature verification. May be null if no certificate is embedded.
    /// </summary>
    public X509Certificate2? SignerCertificate { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the signature is valid.
    /// True if the signature verification passed all checks (integrity, authenticity, and certificate validity).
    /// False if any verification step failed or if verification could not be completed.
    /// </summary>
    public bool IsValid { get; init; }
}
