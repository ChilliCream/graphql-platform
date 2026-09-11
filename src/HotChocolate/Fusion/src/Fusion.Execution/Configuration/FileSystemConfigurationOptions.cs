using System.Security.Cryptography.X509Certificates;

namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// Options for configuring a <see cref="FileSystemFusionConfigurationProvider"/>.
/// </summary>
public sealed class FileSystemConfigurationOptions
{
    /// <summary>
    /// Gets or sets the certificates trusted to sign a Fusion archive (<c>.far</c>) package. When
    /// set, a package archive is rejected unless it carries a valid signature produced by one of
    /// these certificates. When not set, package archives are not required to be signed, but a
    /// manifest present in the archive must still pass its integrity check.
    /// </summary>
    public X509Certificate2Collection? TrustedSigningCertificates { get; set; }
}
