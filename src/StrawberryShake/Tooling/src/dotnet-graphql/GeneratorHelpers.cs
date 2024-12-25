using HotChocolate.Language;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using StrawberryShake.Tools.Configuration;
using static System.IO.Path;

namespace StrawberryShake.Tools;

internal static class GeneratorHelpers
{
    public static string[] GetConfigFiles(string path, IReadOnlyList<string> buildArtifacts)
    {
        const string pattern = "**/.graphqlrc.json";
        var files = Files(path, pattern).Select(t => Combine(path, t)).ToHashSet();
        files.ExceptWith(buildArtifacts);
        return files.ToArray();
    }

    public static string[] GetGraphQLDocuments(
        string path,
        string[] patterns,
        IReadOnlyList<string> buildArtifacts,
        string schemaPath)
    {
        IEnumerable<string> files =
        [
            ..
            patterns
                .SelectMany(pattern => Files(path, pattern))
                .Select(t => Combine(path, t)),
            Combine(path, schemaPath)
        ];

        return files
            .Select(GetFullPath)
            .Distinct()
            .ExceptBy(buildArtifacts, GetFullPath)
            .ToArray();
    }

    public static IReadOnlyList<string> GetBuildArtifacts(string path)
    {
        var artifacts = new List<string>();
        var objDir = Combine(path, "obj");
        var binDir = Combine(path, "bin");

        if (Directory.Exists(objDir))
        {
            artifacts.AddRange(
                Directory.GetFiles(
                    objDir, "*.*",
                    SearchOption.AllDirectories));
        }

        if (Directory.Exists(binDir))
        {
            artifacts.AddRange(
                Directory.GetFiles(
                    binDir, "*.*",
                    SearchOption.AllDirectories));
        }

        return artifacts;
    }

    public static CSharpGeneratorSettings CreateSettings(
        GraphQLConfig config,
        GenerateCommand.GenerateCommandArguments args,
        string rootNamespace)
    {
        var configSettings = config.Extensions.StrawberryShake;
        return new CSharpGeneratorSettings
        {
            ClientName = configSettings.Name,
            Namespace = configSettings.Namespace ?? args.RootNamespace ?? rootNamespace,
            AccessModifier = GetAccessModifier(configSettings.AccessModifier),
            StrictSchemaValidation =
                configSettings.StrictSchemaValidation ?? args.StrictSchemaValidation,
            NoStore = configSettings.NoStore ?? args.NoStore,
            InputRecords = configSettings.Records.Inputs,
            EntityRecords = configSettings.Records.Entities,
            RazorComponents = configSettings.RazorComponents ?? args.RazorComponents,
            SingleCodeFile = configSettings.UseSingleFile ?? args.UseSingleFile,
            RequestStrategy = configSettings.RequestStrategy ?? args.Strategy,
            HashProvider = GetHashProvider(configSettings.HashAlgorithm ?? args.HashAlgorithm),
            TransportProfiles = MapTransportProfiles(configSettings.TransportProfiles),
        };
    }

    private static AccessModifier GetAccessModifier(string? accessModifier)
    {
        if (string.IsNullOrWhiteSpace(accessModifier))
        {
            return AccessModifier.Public;
        }

        return accessModifier switch
        {
            "public" => AccessModifier.Public,
            "internal" => AccessModifier.Internal,
            _ => throw new NotSupportedException($"The access modifier `{accessModifier}` is not supported."),
        };
    }

    private static IDocumentHashProvider GetHashProvider(string hashAlgorithm)
        => hashAlgorithm.ToLowerInvariant() switch
        {
            "md5" => new MD5DocumentHashProvider(HashFormat.Hex),
            "sha1" => new Sha1DocumentHashProvider(HashFormat.Hex),
            "sha256" => new Sha256DocumentHashProvider(HashFormat.Hex),
            _ => throw new NotSupportedException(
                $"The hash algorithm `{hashAlgorithm}` is not supported."),
        };

    private static List<TransportProfile> MapTransportProfiles(
        List<StrawberryShakeSettingsTransportProfile> transportProfileSettings)
    {
        var profiles = new List<TransportProfile>();

        foreach (var settings in transportProfileSettings)
        {
            profiles.Add(
                new TransportProfile(
                    settings.Name,
                    (TransportType)(int)settings.Default,
                    settings.Query.HasValue
                        ? (TransportType)(int)settings.Query
                        : null,
                    settings.Mutation.HasValue
                        ? (TransportType)(int)settings.Mutation
                        : null,
                    settings.Subscription.HasValue
                        ? (TransportType)(int)settings.Subscription
                        : null
                ));
        }

        return profiles;
    }

    private static HashSet<string> Files(string path, string pattern)
        => MatchPatterns(path, [pattern]);

    private static HashSet<string> MatchPatterns(string path, string[] patterns)
    {
        var matcher = new Matcher();

        matcher.AddIncludePatterns(patterns);

        return matcher
            .Execute(new DirectoryInfoWrapper(new DirectoryInfo(path)))
            .Files
            .Select(x => x.Path)
            .ToHashSet();
    }
}
