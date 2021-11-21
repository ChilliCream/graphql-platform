using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Globbing;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using Nerdbank.Streams;
using StrawberryShake.Tools.Configuration;
using StreamJsonRpc;
using IOPath = System.IO.Path;

namespace StrawberryShake.CodeGeneration.CSharp;

internal sealed class Server
{
    private GraphQLConfig _config;
    private StrawberryShakeSettings _settings;
    private string[] _fileNames;

    public async Task<ServerResponse> SetConfiguration(string fileName)
    {
        try
        {
            string json = await File.ReadAllTextAsync(fileName);
            GraphQLConfig config = GraphQLConfig.FromJson(json);

            if (!NameUtils.IsValidGraphQLName(config.Extensions.StrawberryShake.Name))
            {
                return new ServerResponse
                {
                    Errors = new[]
                    {
                            new GeneratorError { Message = "ReportInvalidClientName" }
                        }
                };
            }

            config.Location = fileName;
            _config = config;
            _settings = config.Extensions.StrawberryShake;
            return new ServerResponse();
        }
        catch (Exception ex)
        {
            return new ServerResponse
            {
                Errors = new[] { new GeneratorError { Message = "Unexpected" } }
            };
        }
    }

    public ServerResponse SetDocuments(string[] fileNames)
    {
        _fileNames = fileNames;
        return new ServerResponse();
    }


    public async Task<GeneratorResponse> Generate()
    {
        IReadOnlyList<string> documents = GetDocuments();

        if (documents.Count == 0)
        {
            return new GeneratorResponse { Documents = Array.Empty<SourceDocument>() };
        }

        return GenerateClient(documents);
    }

    private IReadOnlyList<string> GetDocuments()
    {
        string rootDirectory = IOPath.GetDirectoryName(_config.Location) + IOPath.DirectorySeparatorChar;

        var glob = Glob.Parse(_config.Documents);

        return _fileNames
            .Where(t => t.StartsWith(rootDirectory) && glob.IsMatch(t))
            .ToList();
    }

    private GeneratorResponse GenerateClient(IReadOnlyList<string> documents)
    {
        // context.Log.BeginGenerateCode();

        try
        {
            var settings = new CSharpGeneratorSettings
            {
                ClientName = _settings.Name,
                Namespace = "Test.NS",
                RequestStrategy = _settings.RequestStrategy,
                StrictSchemaValidation = _settings.StrictSchemaValidation,
                NoStore = _settings.NoStore,
                InputRecords = _settings.Records.Inputs,
                RazorComponents = _settings.RazorComponents,
                EntityRecords = _settings.Records.Entities,
                SingleCodeFile = _settings.UseSingleFile,
                HashProvider = _settings.HashAlgorithm.ToLowerInvariant() switch
                {
                    "sha1" => new Sha1DocumentHashProvider(HashFormat.Hex),
                    "sha256" => new Sha256DocumentHashProvider(HashFormat.Hex),
                    "md5" => new MD5DocumentHashProvider(HashFormat.Hex),
                    _ => new Sha1DocumentHashProvider(HashFormat.Hex)
                }
            };

            if (_settings.TransportProfiles
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

            return new GeneratorResponse { Documents = result.Documents.ToArray() };
        }
        catch (GraphQLException ex)
        {
            // context.ReportError(ex.Errors);
            return new GeneratorResponse
            {
                Errors = new[] { new GeneratorError { Message = ex.Message } }
            };
        }
        catch (Exception ex)
        {
            // context.Log.Error(ex);
            // context.ReportError(ex);
            return new GeneratorResponse
            {
                Errors = new[] { new GeneratorError { Message = ex.Message } }
            };
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
}