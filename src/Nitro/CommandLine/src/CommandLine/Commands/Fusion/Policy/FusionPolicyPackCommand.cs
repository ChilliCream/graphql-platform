using System.Collections.Immutable;
using System.IO.Compression;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.Policy;

/// <summary>
/// Packs a Rego policy authoring directory (&lt;root&gt;/&lt;package&gt;/*.rego, the matching
/// &lt;root&gt;/&lt;package&gt;.graphql requirements, and shared &lt;root&gt;/lib/*.rego modules) into a
/// v2 Rego policy bundle, using the same scanner and validator the Fusion Packaging reader relies on.
/// </summary>
internal sealed class FusionPolicyPackCommand : Command
{
    public FusionPolicyPackCommand() : base("pack")
    {
        Description = "Pack a Rego policy authoring directory into a Rego policy bundle.";

        Arguments.Add(Opt<FusionPolicyPackRootArgument>.Instance);
        Options.Add(Opt<FusionPolicyPackOutputOption>.Instance);
        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            fusion policy pack ./policies --out ./gateway.far
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fileSystem = services.GetRequiredService<IFileSystem>();

        var root = ResolvePath(fileSystem, parseResult.GetRequiredValue(Opt<FusionPolicyPackRootArgument>.Instance));
        var outPath = ResolvePath(fileSystem, parseResult.GetRequiredValue(Opt<FusionPolicyPackOutputOption>.Instance));

        if (!fileSystem.DirectoryExists(root))
        {
            throw new ExitException($"The root directory '{root.EscapeMarkup()}' does not exist.");
        }

        var bundle = await ReadBundleAsync(fileSystem, root, cancellationToken);
        var isFarOutput = outPath.EndsWith(".far", StringComparison.OrdinalIgnoreCase);
        var farPath = isFarOutput
            ? outPath
            : Path.Combine(Path.GetTempPath(), $"nitro-policy-pack-{Guid.NewGuid():N}.far");

        var farDirectory = Path.GetDirectoryName(farPath);

        if (!string.IsNullOrEmpty(farDirectory) && !fileSystem.DirectoryExists(farDirectory))
        {
            fileSystem.CreateDirectory(farDirectory);
        }

        await using (var stream = fileSystem.CreateFile(farPath))
        {
            using var archive = FusionArchive.Create(stream, leaveOpen: true);
            await archive.SetRegoPolicyBundleAsync(
                bundle, WellKnownVersions.RegoPolicyBundleFormatVersion, cancellationToken);
            await archive.CommitAsync(cancellationToken);
        }

        if (isFarOutput)
        {
            console.Success(
                $"Packed {bundle.Packages.Length} package(s) and {bundle.Libraries.Length} shared "
                + $"module(s) into '{outPath.EscapeMarkup()}'.");
        }
        else
        {
            ExtractBundleToDirectory(fileSystem, farPath, outPath, WellKnownVersions.RegoPolicyBundleFormatVersion);
            fileSystem.DeleteFile(farPath);

            console.Success(
                $"Packed {bundle.Packages.Length} package(s) and {bundle.Libraries.Length} shared "
                + $"module(s) into '{outPath.EscapeMarkup()}'.");
        }

        return ExitCodes.Success;
    }

    private static async Task<RegoPolicyBundle> ReadBundleAsync(
        IFileSystem fileSystem,
        string root,
        CancellationToken cancellationToken)
    {
        var packageFiles = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        var libraryFiles = new List<string>();

        foreach (var file in fileSystem.GetFiles(root, "*.rego", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            var segments = relative.Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.None);

            if (segments.Length != 2)
            {
                throw new ExitException(
                    $"The module '{relative.EscapeMarkup()}' must be directly inside a package "
                    + "directory or 'lib', not nested any deeper and not directly under the root.");
            }

            if (segments[0].Equals("lib", StringComparison.Ordinal))
            {
                libraryFiles.Add(file);
            }
            else
            {
                if (!packageFiles.TryGetValue(segments[0], out var files))
                {
                    files = [];
                    packageFiles.Add(segments[0], files);
                }

                files.Add(file);
            }
        }

        if (packageFiles.Count == 0)
        {
            throw new ExitException($"No Rego package directories were found under '{root.EscapeMarkup()}'.");
        }

        var packages = ImmutableArray.CreateBuilder<RegoPolicyBundlePackage>(packageFiles.Count);

        foreach (var (package, files) in packageFiles)
        {
            var modules = ImmutableArray.CreateBuilder<RegoPolicyBundleModule>(files.Count);

            foreach (var file in files.OrderBy(f => f, StringComparer.Ordinal))
            {
                modules.Add(new RegoPolicyBundleModule(
                    Path.GetFileNameWithoutExtension(file),
                    await fileSystem.ReadAllBytesAsync(file, cancellationToken)));
            }

            // Assigning the ternary's byte[] branch straight into a ReadOnlyMemory<byte>? would take the
            // implicit byte[]-to-ReadOnlyMemory<byte> conversion on the null branch too, producing a
            // "has value" empty memory instead of an actually-absent value; the requirements are only
            // ever wrapped once the file is known to exist.
            var requirementsFile = Path.Combine(root, package + ".graphql");
            ReadOnlyMemory<byte>? requirements = null;

            if (fileSystem.FileExists(requirementsFile))
            {
                requirements = await fileSystem.ReadAllBytesAsync(requirementsFile, cancellationToken);
            }

            packages.Add(new RegoPolicyBundlePackage(package, modules.ToImmutable(), requirements));
        }

        var libraries = ImmutableArray.CreateBuilder<RegoPolicyBundleModule>(libraryFiles.Count);

        foreach (var file in libraryFiles.OrderBy(f => f, StringComparer.Ordinal))
        {
            libraries.Add(new RegoPolicyBundleModule(
                Path.GetFileNameWithoutExtension(file),
                await fileSystem.ReadAllBytesAsync(file, cancellationToken)));
        }

        return new RegoPolicyBundle
        {
            Packages = packages.ToImmutable(),
            Libraries = libraries.ToImmutable()
        };
    }

    // Extracts the bundle's packaged tree (policies/rego/<version>/**) from the .far this command just
    // built into a plain directory, reusing the exact bytes SetRegoPolicyBundleAsync wrote rather than
    // re-deriving the manifest, so there is no second, possibly diverging, packaging code path. Every
    // directory and file write goes through IFileSystem so the command stays testable against a fake.
    private static void ExtractBundleToDirectory(
        IFileSystem fileSystem, string farPath, string outputDirectory, Version version)
    {
        var prefix = $"policies/rego/{version}/";

        using var fileStream = fileSystem.OpenReadStream(farPath);
        using var zip = new ZipArchive(fileStream, ZipArchiveMode.Read);

        foreach (var entry in zip.Entries)
        {
            if (!entry.FullName.StartsWith(prefix, StringComparison.Ordinal)
                || entry.FullName.EndsWith("/", StringComparison.Ordinal))
            {
                continue;
            }

            var relative = entry.FullName[prefix.Length..];
            var destination = Path.Combine(outputDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
            var destinationDirectory = Path.GetDirectoryName(destination);

            if (!string.IsNullOrEmpty(destinationDirectory) && !fileSystem.DirectoryExists(destinationDirectory))
            {
                fileSystem.CreateDirectory(destinationDirectory);
            }

            using var entryStream = entry.Open();
            using var destinationStream = fileSystem.CreateFile(destination);
            entryStream.CopyTo(destinationStream);
        }
    }

    private static string ResolvePath(IFileSystem fileSystem, string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(fileSystem.GetCurrentDirectory(), path);
}
