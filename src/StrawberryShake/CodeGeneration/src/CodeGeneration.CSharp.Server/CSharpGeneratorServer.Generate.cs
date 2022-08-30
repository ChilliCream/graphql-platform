using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GlobExpressions;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Utilities;
using StrawberryShake.Tools.Configuration;
using static System.IO.Path;
using static StrawberryShake.CodeGeneration.CSharp.ErrorHelper;
using static StrawberryShake.CodeGeneration.CSharp.ServerResources;
using Path = System.IO.Path;

namespace StrawberryShake.CodeGeneration.CSharp;

public static partial class CSharpGeneratorServer
{
    private static readonly SHA256 _sha256 = SHA256.Create();

    private static async Task<GeneratorResponse> GenerateAsync(GeneratorRequest request)
    {
        try
        {
            var settings = await LoadSettingsAsync(request);
            var documents = GetMatchingDocuments(request, settings);

            if (settings.RequestStrategy == RequestStrategy.PersistedQuery)
            {
                if (settings.PersistedQueryDirectory is null)
                {
                    settings.RequestStrategy = RequestStrategy.Default;
                }
            }

            var result = CSharpGenerator.Generate(documents, settings);

            await TryWriteCSharpFilesAsync(result.Documents, settings);
            await TryWriteRazorFilesAsync(result.Documents, settings);
            await TryWritePersistedQueriesAsync(result.Documents, settings);
            await TryWritePersistedQueriesJsonAsync(result.Documents, settings);

            return CreateResponse(
                request.Option is RequestOptions.GenerateCSharpClient
                    ? Array.Empty<SourceDocument>()
                    : result.Documents.Where(t => t.Kind is SourceDocumentKind.CSharp).ToList(),
                ConvertErrors(result.Errors));
        }
        catch (GraphQLException ex)
        {
            return ExceptionToError(ex);
        }
        catch (Exception ex)
        {
            return ExceptionToError(ex);
        }
    }

    private static async Task TryWriteCSharpFilesAsync(
        IReadOnlyList<SourceDocument> documents,
        CSharpGeneratorServerSettings settings)
    {
        if (settings.Option is not RequestOptions.GenerateCSharpClient)
        {
            return;
        }

        var generatedDirectory = IsPathRooted(settings.OutputDirectoryName)
            ? settings.OutputDirectoryName
            : Combine(settings.RootDirectoryName, settings.OutputDirectoryName);

        if (!Directory.Exists(generatedDirectory))
        {
            Directory.CreateDirectory(generatedDirectory);
        }

        var generatedFiles = new HashSet<string>();

        foreach (var document in
            documents.Where(t => t.Kind is SourceDocumentKind.CSharp))
        {
            var fileName = Combine(generatedDirectory, $"{document.Name}.g.cs");
            var exists = File.Exists(fileName);

            generatedFiles.Add(fileName);

            if (!exists || !Compare(fileName, document))
            {
                if (exists)
                {
                    File.Delete(fileName);
                }

                await File.WriteAllTextAsync(fileName, document.SourceText);
            }
        }

        foreach (var file in Directory.GetFiles(generatedDirectory, "*.components.g.cs"))
        {
            generatedFiles.Add(file);
        }

        foreach (var file in Directory.GetFiles(generatedDirectory, "*.g.cs"))
        {
            if (!generatedFiles.Contains(file))
            {
                File.Delete(file);
            }
        }
    }

    private static async Task TryWriteRazorFilesAsync(
        IReadOnlyList<SourceDocument> documents,
        CSharpGeneratorServerSettings settings)
    {
        if (!settings.RazorComponents)
        {
            return;
        }

        var generatedDirectory = Combine(settings.RootDirectoryName, settings.OutputDirectoryName);

        if (!Directory.Exists(generatedDirectory))
        {
            Directory.CreateDirectory(generatedDirectory);
        }

        var generatedFiles = new HashSet<string>();

        foreach (var document in
            documents.Where(t => t.Kind is SourceDocumentKind.Razor))
        {
            var fileName = Combine(generatedDirectory, $"{document.Name}.components.g.cs");
            var exists = File.Exists(fileName);

            generatedFiles.Add(fileName);

            if (!exists || !Compare(fileName, document))
            {
                if (exists)
                {
                    File.Delete(fileName);
                }

                await File.WriteAllTextAsync(fileName, document.SourceText);
            }
        }

        foreach (var file in Directory.GetFiles(generatedDirectory, "*.components.g.cs"))
        {
            if (!generatedFiles.Contains(file))
            {
                File.Delete(file);
            }
        }
    }

    private static async Task TryWritePersistedQueriesAsync(
        IReadOnlyList<SourceDocument> documents,
        CSharpGeneratorServerSettings settings)
    {
        if (settings.RequestStrategy is not RequestStrategy.PersistedQuery ||
            settings.Option is not RequestOptions.Default and
                not RequestOptions.ExportPersistedQueries)
        {
            return;
        }

        var persistedQueryDirectory = settings.PersistedQueryDirectory!;

        if (!Directory.Exists(persistedQueryDirectory))
        {
            Directory.CreateDirectory(persistedQueryDirectory);
        }

        ClearPersistedQueryDirectory(persistedQueryDirectory);

        foreach (var document in
            documents.Where(t => t.Kind is SourceDocumentKind.GraphQL))
        {
            var fileName = Combine(persistedQueryDirectory, $"{document.Name}.graphql");
            await File.WriteAllTextAsync(fileName, document.SourceText);
        }
    }

    private static async Task TryWritePersistedQueriesJsonAsync(
        IReadOnlyList<SourceDocument> documents,
        CSharpGeneratorServerSettings settings)
    {
        if (settings.RequestStrategy is not RequestStrategy.PersistedQuery ||
            settings.Option is not RequestOptions.ExportPersistedQueriesJson)
        {
            return;
        }

        var persistedQueryDir = settings.PersistedQueryDirectory!;
        var persistedQueryFile = Combine(persistedQueryDir, "persisted-queries.json");

        if (!Directory.Exists(persistedQueryDir))
        {
            Directory.CreateDirectory(persistedQueryDir);
        }

        if (File.Exists(persistedQueryFile))
        {
            File.Delete(persistedQueryFile);
        }

        var files = new Dictionary<string, string>();

        foreach (var document in
            documents.Where(t => t.Kind is SourceDocumentKind.GraphQL))
        {
            var hash = BitConverter.ToString(ComputeHash(document)).Replace("-", "");
            files[hash] = document.SourceText;
        }

        await File.WriteAllBytesAsync(
            persistedQueryFile,
            JsonSerializer.SerializeToUtf8Bytes(files));
    }

    private static async Task<CSharpGeneratorServerSettings> LoadSettingsAsync(
        GeneratorRequest request)
    {
        try
        {
            var json = await File.ReadAllTextAsync(request.ConfigFileName);
            var config = GraphQLConfig.FromJson(json);

            if (!config.Extensions.StrawberryShake.Name.IsValidGraphQLName())
            {
                throw new GraphQLException(CSharpGeneratorServer_ClientName_Invalid);
            }

            var generatorSettings = new CSharpGeneratorServerSettings
            {
                RootDirectoryName = request.RootDirectory,
                OutputDirectoryName = config.Extensions.StrawberryShake.OutputDirectoryName,
                ClientName = config.Extensions.StrawberryShake.Name,
                Namespace = config.Extensions.StrawberryShake.Namespace ??
                    request.DefaultNamespace ??
                    "StrawberryShake.Generated",
                RequestStrategy = config.Extensions.StrawberryShake.RequestStrategy,
                StrictSchemaValidation =
                    config.Extensions.StrawberryShake.StrictSchemaValidation
                        ?? true,
                NoStore = config.Extensions.StrawberryShake.NoStore ?? true,
                InputRecords = config.Extensions.StrawberryShake.Records.Inputs,
                RazorComponents = config.Extensions.StrawberryShake.RazorComponents ?? false,
                EntityRecords = config.Extensions.StrawberryShake.Records.Entities,
                SingleCodeFile = config.Extensions.StrawberryShake.UseSingleFile ?? true,
                Documents = config.Documents,
                PersistedQueryDirectory = request.PersistedQueryDirectory,
                HashProvider =
                    (config.Extensions.StrawberryShake.HashAlgorithm?.ToLowerInvariant() ?? "md5")
                        switch
                        {
                            "sha1" => new Sha1DocumentHashProvider(HashFormat.Hex),
                            "sha256" => new Sha256DocumentHashProvider(HashFormat.Hex),
                            "md5" => new MD5DocumentHashProvider(HashFormat.Hex),
                            _ => new Sha1DocumentHashProvider(HashFormat.Hex)
                        },
                Option = request.Option
            };

            if (config.Extensions.StrawberryShake.TransportProfiles
                .Where(t => !string.IsNullOrEmpty(t.Name))
                .ToList() is { Count: > 0 } profiles)
            {
                generatorSettings.TransportProfiles.Clear();

                foreach (var profile in profiles)
                {
                    generatorSettings.TransportProfiles.Add(
                        new TransportProfile(
                            profile.Name,
                            profile.Default,
                            profile.Query,
                            profile.Mutation,
                            profile.Subscription));
                }
            }

            switch (request.Option)
            {
                case RequestOptions.ExportPersistedQueries:
                    generatorSettings.RazorComponents = false;
                    generatorSettings.RequestStrategy = RequestStrategy.PersistedQuery;
                    break;

                case RequestOptions.ExportPersistedQueriesJson:
                    generatorSettings.RazorComponents = false;
                    generatorSettings.RequestStrategy = RequestStrategy.PersistedQuery;
                    break;

                case RequestOptions.GenerateRazorComponent:
                    generatorSettings.RazorComponents = true;
                    generatorSettings.RequestStrategy = RequestStrategy.Default;
                    break;

                case RequestOptions.GenerateCSharpClient:
                    generatorSettings.RazorComponents = false;
                    generatorSettings.RequestStrategy = RequestStrategy.Default;
                    break;
            }

            return generatorSettings;
        }
        catch (Exception ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }

    private static IReadOnlyList<string> GetMatchingDocuments(
        GeneratorRequest request,
        CSharpGeneratorServerSettings settings)
    {
        var rootDirectory = request.RootDirectory;

        var files = Glob.Files(rootDirectory, settings.Documents)
            .Select(t => Combine(rootDirectory, t))
            .ToArray();

        return files;
    }

    private static GeneratorResponse CreateResponse(
        IReadOnlyList<SourceDocument> sourceDocuments,
        IReadOnlyList<GeneratorError> errors)
    {
        var generatorDocuments = new List<GeneratorDocument>();

        foreach (var sourceDocument in sourceDocuments)
        {
            generatorDocuments.Add(
                new GeneratorDocument(
                    sourceDocument.Name,
                    sourceDocument.SourceText,
                    (GeneratorDocumentKind)(int)sourceDocument.Kind,
                    sourceDocument.Hash,
                    sourceDocument.Path));
        }

        return new GeneratorResponse(generatorDocuments, errors);
    }

    private static void ClearPersistedQueryDirectory(string persistedQueryDirectory)
    {
        foreach (var fileName in Directory.GetFiles(persistedQueryDirectory, "*.graphql"))
        {
            try
            {
                File.Delete(fileName);
            }
            catch
            {
                // We ignore if we cannot delete a file.
                // We will report on write that there is and issue.
            }
        }
    }

    private static bool Compare(string fileName, SourceDocument document)
        => ComputeHash(fileName).SequenceEqual(ComputeHash(document));

    private static byte[] ComputeHash(string fileName)
    {
        using var stream = File.OpenRead(fileName);
        return _sha256.ComputeHash(stream);
    }

    private static byte[] ComputeHash(SourceDocument document)
    {
        var buffer = Encoding.UTF8.GetBytes(document.SourceText);
        return _sha256.ComputeHash(buffer);
    }
}
