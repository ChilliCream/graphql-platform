namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Represents the result of signature verification for a Fusion Archive.
/// </summary>
public enum SignatureVerificationResult
{
    /// <summary>
    /// The signature is valid and all integrity checks passed.
    /// The archive has not been tampered with since signing.
    /// </summary>
    Valid,

    /// <summary>
    /// The archive is not digitally signed.
    /// </summary>
    NotSigned,

    /// <summary>
    /// One or more files referenced in the signature manifest are missing from the archive.
    /// </summary>
    FilesMissing,

    /// <summary>
    /// One or more files have been modified since the archive was signed.
    /// The computed file hashes do not match those in the signature manifest.
    /// </summary>
    FilesModified,

    /// <summary>
    /// The signature manifest itself is corrupted or has been tampered with.
    /// The manifest hash does not match the computed hash.
    /// </summary>
    ManifestCorrupted,

    /// <summary>
    /// The cryptographic signature is invalid or was not created by the expected certificate.
    /// This indicates either signature corruption or use of an incorrect verification key.
    /// </summary>
    InvalidSignature,

    /// <summary>
    /// Signature verification failed due to an unexpected error during the verification process.
    /// This may indicate archive corruption or an internal verification error.
    /// </summary>
    VerificationFailed
}
