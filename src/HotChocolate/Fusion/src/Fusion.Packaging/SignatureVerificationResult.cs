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
    /// The root content manifest is missing, so the archive contents cannot be verified.
    /// </summary>
    ManifestMissing,

    /// <summary>
    /// A file is present in the archive but not listed in the content manifest.
    /// The manifest never lists the manifest itself or the contents of the signature directory.
    /// </summary>
    UnlistedFile,

    /// <summary>
    /// A file that is present in the archive and listed in the content manifest does not match
    /// the digest recorded for it.
    /// </summary>
    FilesModified,

    /// <summary>
    /// The content manifest declares a digest algorithm other than <c>sha256</c>, which is the only
    /// supported value.
    /// </summary>
    UnsupportedAlgorithm,

    /// <summary>
    /// The cryptographic signature is invalid, was not created by the expected certificate, or
    /// does not match the current content manifest.
    /// </summary>
    InvalidSignature,

    /// <summary>
    /// Signature verification failed due to an unexpected error during the verification process.
    /// This may indicate archive corruption or an internal verification error.
    /// </summary>
    VerificationFailed
}
