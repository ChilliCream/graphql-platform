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

namespace StrawberryShake.CodeGeneration.CSharp;

public sealed class Server
{
    private GraphQLConfig? _config;
    private string[]? _fileNames;

    [JsonRpcMethod("generator/SetConfiguration")]
    public async Task<ServerResponse> SetConfigurationAsync(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return ServerResponse.Error("The `fileName` cannot be null or empty.");
        }

        try
        {
            var json = await File.ReadAllTextAsync(fileName);
            var config = GraphQLConfig.FromJson(json);

            if (!NameUtils.IsValidGraphQLName(config.Extensions.StrawberryShake.Name))
            {
                return ServerResponse.Error("The client name is invalid.");
            }

            config.Location = fileName;
            _config = config;
            return ServerResponse.Success;
        }
        catch
        {
            return ServerResponse.Error("Unexpected Error.");
        }
    }

    [JsonRpcMethod("generator/SetDocuments")]
    public ServerResponse SetDocuments(string[] fileNames)
    {
        if(fileNames is null || fileNames.Length == 0)
        {
            return ServerResponse.Error("There must be at least one graphql document specified.");
        }

        _fileNames = fileNames;
        return ServerResponse.Success;
    }

    [JsonRpcMethod("generator/Generate")]
    public Task<GeneratorResponse> GenerateAsync()
    {
        if (_config is null)
        {
            return Task.FromResult(
                GeneratorResponse.Error("The configuration must be specified first."));
        }

        if (_fileNames is null || _fileNames.Length == 0)
        {
            return Task.FromResult(
                GeneratorResponse.Error("There must be at least one graphql document specified."));
        }

        var context = new GeneratorContext(
            _config,
            _config.Extensions.StrawberryShake,
            _fileNames);

        IReadOnlyList<string> documents = GetDocuments(context);

        if (documents.Count == 0)
        {
            return Task.FromResult(GeneratorResponse.Success());
        }

        return Task.Run(() => GenerateClient(context, documents));
    }

    private IReadOnlyList<string> GetDocuments(GeneratorContext context)
    {
        var rootDirectory = GetDirectoryName(context.Config.Location) + DirectorySeparatorChar;

        var glob = Glob.Parse(context.Config.Documents);

        return context.FileNames
            .Where(t => t.StartsWith(rootDirectory) && glob.IsMatch(t))
            .ToList();
    }

    private GeneratorResponse GenerateClient(
        GeneratorContext context,
        IReadOnlyList<string> documents)
    {
        // context.Log.BeginGenerateCode();

        try
        {
            var settings = new CSharpGeneratorSettings
            {
                ClientName = context.Settings.Name,
                Namespace = "Test.NS",
                RequestStrategy = context.Settings.RequestStrategy,
                StrictSchemaValidation = context.Settings.StrictSchemaValidation,
                NoStore = context.Settings.NoStore,
                InputRecords = context.Settings.Records.Inputs,
                RazorComponents = context.Settings.RazorComponents,
                EntityRecords = context.Settings.Records.Entities,
                SingleCodeFile = context.Settings.UseSingleFile,
                HashProvider = context.Settings.HashAlgorithm.ToLowerInvariant() switch
                {
                    "sha1" => new Sha1DocumentHashProvider(HashFormat.Hex),
                    "sha256" => new Sha256DocumentHashProvider(HashFormat.Hex),
                    "md5" => new MD5DocumentHashProvider(HashFormat.Hex),
                    _ => new Sha1DocumentHashProvider(HashFormat.Hex)
                }
            };

            if (context.Settings.TransportProfiles
                .Where(t => !string.IsNullOrEmpty(t.Name))
                .ToList() is { Count: > 0 } profiles)
            {
                settings.TransportProfiles.Clear();

                foreach (var profile in profiles)
                {
                    settings.TransportProfiles.Add(
                        new TransportProfile(
                            profile.Name,
                            profile.Default,
                            profile.Query,
                            profile.Mutation,
                            profile.Subscription));
                }
            }

            string? persistedQueryDirectory = null;
            // var persistedQueryDirectory = context.GetPersistedQueryDirectory();
            // context.Log.SetGeneratorSettings(settings);
            //context.Log.SetPersistedQueryLocation(persistedQueryDirectory);

            if (settings.RequestStrategy == RequestStrategy.PersistedQuery &&
                persistedQueryDirectory is null)
            {
                settings.RequestStrategy = RequestStrategy.Default;
            }

            CSharpGeneratorResult result = CSharpGenerator.Generate(documents, settings);

            return GeneratorResponse.Success(result.Documents.ToArray());
        }
        catch (GraphQLException ex)
        {
            // context.ReportError(ex.Errors);

            return GeneratorResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            // context.Log.Error(ex);
            // context.ReportError(ex);
            return GeneratorResponse.Error(ex.Message);
        }
        finally
        {
            // context.Log.EndGenerateCode();
        }
    }

    public static async Task RunAsync()
    {
        await LogAsync("StrawberryShake is initializing.");
        await using Stream stream = FullDuplexStream.Splice(
            Console.OpenStandardInput(),
            Console.OpenStandardOutput());
        var jsonRpc = JsonRpc.Attach(stream, new Server());
        await LogAsync("StrawberryShake is ready for requests.");
        await jsonRpc.Completion;
        await LogAsync("StrawberryShake terminated.");
    }

    private static Task LogAsync(string s)
        => Console.Error.WriteLineAsync(s);

    private readonly record struct GeneratorContext(
        GraphQLConfig Config,
        StrawberryShakeSettings Settings,
        string[] FileNames);
}
