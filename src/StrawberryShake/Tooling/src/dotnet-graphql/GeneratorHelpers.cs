using HotChocolate.Language;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using StrawberryShake.Tools.Configuration;
using static System.IO.Path;
using static GlobExpressions.Glob;

namespace StrawberryShake.Tools;

internal static class GeneratorHelpers
{
    public static string[] GetConfigFiles(string path)
    {
        const string pattern = "**/.graphqlrc.json";
        var binPath = Combine(path, "bin");
        var bin = Files(binPath, pattern).Select(t => Combine(binPath, t)).ToArray();
        var objPath = Combine(path, "obj");
        var obj = Files(objPath, pattern).Select(t => Combine(objPath, t)).ToArray();
        var files = Files(path, pattern).Select(t => Combine(path, t)).ToHashSet();

        files.ExceptWith(bin);
        files.ExceptWith(obj);

        return files.ToArray();
    }

    public static string[] GetGraphQLDocuments(string path, string pattern)
    {
        var binPath = Combine(path, "bin");
        var bin = Files(binPath, pattern).Select(t => Combine(binPath, t)).ToArray();
        var objPath = Combine(path, "obj");
        var obj = Files(objPath, pattern).Select(t => Combine(objPath, t)).ToArray();
        var files = Files(path, pattern).Select(t => Combine(path, t)).ToHashSet();

        files.ExceptWith(bin);
        files.ExceptWith(obj);

        return files.ToArray();
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
            StrictSchemaValidation =
                configSettings.StrictSchemaValidation ?? args.StrictSchemaValidation,
            NoStore = configSettings.NoStore ?? args.NoStore,
            InputRecords = configSettings.Records.Inputs,
            EntityRecords = configSettings.Records.Entities,
            RazorComponents = configSettings.RazorComponents ?? args.RazorComponents,
            SingleCodeFile = configSettings.UseSingleFile ?? args.UseSingleFile,
            RequestStrategy = configSettings.RequestStrategy,
            HashProvider = GetHashProvider(configSettings.HashAlgorithm ?? args.HashAlgorithm),
            TransportProfiles = MapTransportProfiles(configSettings.TransportProfiles)
        };
    }

    private static IDocumentHashProvider GetHashProvider(string hashAlgorithm)
        => hashAlgorithm.ToLowerInvariant() switch
        {
            "md5" => new MD5DocumentHashProvider(HashFormat.Hex),
            "sha1" => new Sha1DocumentHashProvider(HashFormat.Hex),
            "sha256" => new Sha256DocumentHashProvider(HashFormat.Hex),
            _ => throw new NotSupportedException(
                $"The hash algorithm `{hashAlgorithm}` is not supported.")
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
}
