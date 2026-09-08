namespace HotChocolate.Fusion.Packaging;

internal static class ThrowHelper
{
    public static ArgumentException RegoPolicyPackageMustMatchName()
        => new(
            "The Rego policy package must be a single segment that matches the policy name.",
            "policy");

    public static ArgumentException RegoPolicyRequirementsMustBeSelectionSetOrFragment()
        => new(
            "The Fusion archive format requires Rego policy requirements to be a bare selection set "
            + "or a single fragment definition.",
            "requirements");

    public static InvalidOperationException SignatureMustBeRemovedBeforeCommit()
        => new("The archive contains a stale signature. Call RemoveSignatureAsync before committing changes.");

    public static ArgumentException RegoPolicyBundleMustHaveAtLeastOnePackage()
        => new("The Rego policy bundle must contain at least one package.", "bundle");

    public static ArgumentException RegoPolicyBundlePackageMustHaveAtLeastOneModule(string package)
        => new($"The Rego policy bundle package '{package}' must contain at least one module.", "bundle");

    public static ArgumentException RegoPolicyBundlePackageNameInvalid(string package)
        => new(
            $"The Rego policy bundle package name '{package}' must be a single valid Rego package segment.",
            "bundle");

    public static ArgumentException RegoPolicyBundlePackageDuplicate(string package)
        => new($"The Rego policy bundle contains the package '{package}' more than once.", "bundle");

    public static ArgumentException RegoPolicyBundleModuleNameInvalid(string package, string module)
        => new(
            $"The module name '{module}' in Rego policy bundle package '{package}' is not a valid path segment.",
            "bundle");

    public static ArgumentException RegoPolicyBundleModuleDuplicate(string package, string module)
        => new(
            $"The Rego policy bundle package '{package}' contains the module '{module}' more than once.",
            "bundle");

    public static ArgumentException RegoPolicyBundleModulePackageMismatch(string package, string module)
        => new(
            $"The module '{module}' in Rego policy bundle package '{package}' declares a different "
            + "Rego package than its containing package.",
            "bundle");

    public static ArgumentException RegoPolicyBundlePackageEntrypointCountInvalid(string package, int count)
        => new(
            count == 0
                ? $"The Rego policy bundle package '{package}' declares no entrypoint decision. Exactly "
                    + "one of its modules must declare at least one rule annotated '# METADATA' / "
                    + "'entrypoint: true'."
                : $"The Rego policy bundle package '{package}' declares entrypoint decisions in more than "
                    + "one module. Exactly one module per package may declare entrypoint decisions; move "
                    + "the remaining rules into that module or split them into a separate package.",
            "bundle");

    public static ArgumentException RegoPolicyBundleLibraryNameInvalid(string library)
        => new($"The library name '{library}' is not a valid path segment.", "bundle");

    public static ArgumentException RegoPolicyBundleLibraryDuplicate(string library)
        => new($"The Rego policy bundle contains the library '{library}' more than once.", "bundle");

    public static ArgumentException RegoPolicyBundlePathCollision(string path)
        => new($"The Rego policy bundle path '{path}' is used by more than one payload.", "bundle");

    public static InvalidDataException RegoPolicyBundleManifestMissing(Version version)
        => new($"The Rego policy bundle format '{version}' is missing its manifest.json.");

    public static InvalidDataException RegoPolicyBundleManifestInvalid(Version version, string reason)
        => new($"The Rego policy bundle manifest for format '{version}' is invalid: {reason}");

    public static InvalidDataException RegoPolicyBundleManifestFormatVersionMismatch(
        Version version,
        int manifestFormatVersion)
        => new(
            $"The Rego policy bundle manifest at 'policies/rego/{version}/manifest.json' declares "
            + $"formatVersion {manifestFormatVersion}, which does not match its directory version '{version}'.");

    public static InvalidDataException RegoPolicyBundlePathInvalid(string path)
        => new($"The Rego policy bundle manifest references the invalid path '{path}'.");

    public static InvalidDataException RegoPolicyBundlePathMissing(string path)
        => new($"The Rego policy bundle manifest references the path '{path}', which is not present in the archive.");

    public static InvalidDataException RegoPolicyBundlePathUnlisted(string path)
        => new($"The Rego policy bundle contains the path '{path}', which is not listed in its manifest.");

    public static InvalidDataException RegoPolicyBundleHashMismatch(string path)
        => new($"The Rego policy bundle path '{path}' does not match the digest recorded in its manifest.");

    public static InvalidDataException RegoPolicyBundlePolicyNameDuplicate(string name)
        => new($"The Rego policy bundle manifest lists the policy '{name}' more than once.");

    public static InvalidDataException RegoPolicyBundleLibraryPathDuplicate(string path)
        => new($"The Rego policy bundle manifest lists the library '{path}' more than once.");

    public static InvalidDataException RegoPolicyBundleIdentityMismatch(string name, string reason)
        => new($"The Rego policy bundle manifest entry '{name}' does not match its scanned module: {reason}");
}
