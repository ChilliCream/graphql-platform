using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Globbing;
using HotChocolate;
using HotChocolate.Language;
using Nerdbank.Streams;
using StrawberryShake.Tools.Configuration;
using StreamJsonRpc;
using static System.IO.Path;
using static StrawberryShake.CodeGeneration.CSharp.ErrorHelper;

namespace StrawberryShake.CodeGeneration.CSharp;

public sealed class CSharpGeneratorServer
{
    [JsonRpcMethod("generator/Generate")]
    public async Task<GeneratorResponse> GenerateAsync(GeneratorRequest request)
    {
        try
        {
            CSharpGeneratorSettings settings = await LoadSettingsAsync(request);
            IReadOnlyList<string> documents = GetMatchingDocuments(request, settings);

            if (settings.RequestStrategy == RequestStrategy.PersistedQuery &&
                request.PersistedQueryDirectory is null)
            {
                settings.RequestStrategy = RequestStrategy.Default;
            }

            CSharpGeneratorResult result = CSharpGenerator.Generate(documents, settings);
            return CreateSuccessResponse(result.Documents);
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

    private async Task<CSharpGeneratorSettings> LoadSettingsAsync(GeneratorRequest request)
    {
        try
        {
            var json = await File.ReadAllTextAsync(request.ConfigFileName);
            var config = GraphQLConfig.FromJson(json);

            if (!NameUtils.IsValidGraphQLName(config.Extensions.StrawberryShake.Name))
            {
                throw new GraphQLException("The client name is invalid.");
            }

            var generatorSettings = new CSharpGeneratorSettings
            {
                ClientName = config.Extensions.StrawberryShake.Name,
                Namespace = request.Namespace ??
                    config.Extensions.StrawberryShake.Namespace ??
                    "StrawberryShake.Generated",
                RequestStrategy = config.Extensions.StrawberryShake.RequestStrategy,
                StrictSchemaValidation = config.Extensions.StrawberryShake.StrictSchemaValidation,
                NoStore = config.Extensions.StrawberryShake.NoStore,
                InputRecords = config.Extensions.StrawberryShake.Records.Inputs,
                RazorComponents = config.Extensions.StrawberryShake.RazorComponents,
                EntityRecords = config.Extensions.StrawberryShake.Records.Entities,
                SingleCodeFile = config.Extensions.StrawberryShake.UseSingleFile,
                Documents = config.Documents,
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

    private IReadOnlyList<string> GetMatchingDocuments(
        GeneratorRequest request,
        CSharpGeneratorSettings settings)
    {
        var rootDirectory = GetDirectoryName(request.ConfigFileName) + DirectorySeparatorChar;

        var glob = Glob.Parse(settings.Documents);

        return request.DocumentFileNames
            .Where(t => t.StartsWith(rootDirectory) && glob.IsMatch(t))
            .ToList();
    }

    private static GeneratorResponse CreateSuccessResponse(
        IReadOnlyList<SourceDocument> sourceDocuments)
    {
        var generatorDocuments = new GeneratorDocument[sourceDocuments.Count];

        for (var i = 0; i < sourceDocuments.Count; i++)
        {
            SourceDocument sourceDocument = sourceDocuments[i];

            generatorDocuments[i] = new GeneratorDocument(
                sourceDocument.Name,
                sourceDocument.SourceText,
                (GeneratorDocumentKind)(int)sourceDocument.Kind,
                sourceDocument.Hash,
                sourceDocument.Path);
        }

        return new GeneratorResponse(generatorDocuments);
    }

    public static async Task RunAsync()
    {
        await using Stream stream = FullDuplexStream.Splice(
            Console.OpenStandardInput(),
            Console.OpenStandardOutput());
        var jsonRpc = JsonRpc.Attach(stream, new CSharpGeneratorServer());
        await jsonRpc.Completion;
    }
}
