using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DotNet.Globbing;
using HotChocolate;
using HotChocolate.Language;
using StrawberryShake.Tools.Configuration;
using Path = System.IO.Path;
using static System.IO.Path;
using static StrawberryShake.CodeGeneration.CSharp.ErrorHelper;

namespace StrawberryShake.CodeGeneration.CSharp;


public static partial class CSharpGeneratorServer
{
    private static readonly SHA256 _sha256 = SHA256.Create();

    private static async Task<GeneratorResponse> GenerateAsync(GeneratorRequest request)
    {
        try
        {
            CSharpGeneratorServerSettings settings = await LoadSettingsAsync(request);
            IReadOnlyList<string> documents = GetMatchingDocuments(request, settings);

            if (settings.RequestStrategy == RequestStrategy.PersistedQuery)
            {
                if (settings.PersistedQueryDirectory is null)
                {
                    settings.RequestStrategy = RequestStrategy.Default;
                }
                else
                {
                    if (!Directory.Exists(settings.PersistedQueryDirectory))
                    {
                        Directory.CreateDirectory(settings.PersistedQueryDirectory);
                    }

                    ClearPersistedQueryDirectory(settings.PersistedQueryDirectory);
                }
            }

            CSharpGeneratorResult result = CSharpGenerator.Generate(documents, settings);

            await WriteRazorFilesAsync(result.Documents, settings);

            return CreateResponse(
                result.Documents.Where(t => t.Kind is SourceDocumentKind.CSharp).ToList(),
                ConvertErrors(result.Errors),
                settings.PersistedQueryDirectory);
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

    private static async Task WriteRazorFilesAsync(
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

        var razorFiles = new Dictionary<string, SourceDocument>();

        foreach (SourceDocument document in
            documents.Where(t => t.Kind is SourceDocumentKind.Razor))
        {
            var fileName = Combine(generatedDirectory, $"{document.Name}.component.g.cs");
            var exists = File.Exists(fileName);

            razorFiles[fileName] = document;

            if (!exists || !Compare(fileName, document))
            {
                if (exists)
                {
                    File.Delete(fileName);
                }

                await File.WriteAllTextAsync(fileName, document.SourceText);
            }
        }

        foreach (var file in Directory.GetFiles(generatedDirectory, "*.*"))
        {
            if (!razorFiles.ContainsKey(file))
            {
                File.Delete(file);
            }
        }
    }

    private static async Task<CSharpGeneratorServerSettings> LoadSettingsAsync(
        GeneratorRequest request)
    {
        try
        {
            var json = await File.ReadAllTextAsync(request.ConfigFileName);
            var config = GraphQLConfig.FromJson(json);

            if (!NameUtils.IsValidGraphQLName(config.Extensions.StrawberryShake.Name))
            {
                throw new GraphQLException(ServerResources.CSharpGeneratorServer_ClientName_Invalid);
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
                StrictSchemaValidation = config.Extensions.StrawberryShake.StrictSchemaValidation,
                NoStore = config.Extensions.StrawberryShake.NoStore,
                InputRecords = config.Extensions.StrawberryShake.Records.Inputs,
                RazorComponents = config.Extensions.StrawberryShake.RazorComponents,
                EntityRecords = config.Extensions.StrawberryShake.Records.Entities,
                SingleCodeFile = config.Extensions.StrawberryShake.UseSingleFile,
                Documents = config.Documents,
                PersistedQueryDirectory = request.PersistedQueryDirectory,
                HashProvider = config.Extensions.StrawberryShake.HashAlgorithm.ToLowerInvariant()
                    switch
                {
                    "sha1" => new Sha1DocumentHashProvider(HashFormat.Hex),
                    "sha256" => new Sha256DocumentHashProvider(HashFormat.Hex),
                    "md5" => new MD5DocumentHashProvider(HashFormat.Hex),
                    _ => new Sha1DocumentHashProvider(HashFormat.Hex)
                }
            };

            if (config.Extensions.StrawberryShake.TransportProfiles
                .Where(t => !string.IsNullOrEmpty(t.Name))
                .ToList() is { Count: > 0 } profiles)
            {
                generatorSettings.TransportProfiles.Clear();

                foreach (StrawberryShakeSettingsTransportProfile profile in profiles)
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
        var rootDirectory = request.RootDirectory + DirectorySeparatorChar;

        var glob = Glob.Parse(settings.Documents);

        return request.DocumentFileNames
            .Where(t => t.StartsWith(rootDirectory) && glob.IsMatch(t))
            .ToList();
    }

    private static GeneratorResponse CreateResponse(
        IReadOnlyList<SourceDocument> sourceDocuments,
        IReadOnlyList<GeneratorError> errors,
        string? persistedQueryDirectory)
    {
        var generatorDocuments = new List<GeneratorDocument>();

        foreach (SourceDocument sourceDocument in sourceDocuments)
        {
            if (sourceDocument.Kind is SourceDocumentKind.GraphQL)
            {
                if (persistedQueryDirectory is not null)
                {
                    File.WriteAllText(
                        Combine(persistedQueryDirectory, sourceDocument.Name),
                        sourceDocument.SourceText,
                        Encoding.UTF8);
                }
            }
            else
            {
                generatorDocuments.Add(
                    new GeneratorDocument(
                        sourceDocument.Name,
                        sourceDocument.SourceText,
                        (GeneratorDocumentKind)(int)sourceDocument.Kind,
                        sourceDocument.Hash,
                        sourceDocument.Path));
            }
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
        using FileStream stream = File.OpenRead(fileName);
        return _sha256.ComputeHash(stream);
    }

    private static byte[] ComputeHash(SourceDocument document)
    {
        var buffer = Encoding.UTF8.GetBytes(document.SourceText);
        return _sha256.ComputeHash(buffer);
    }
}
