using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal sealed class FusionSourceSchemaInitCommand : Command
{
    private const string DefaultUrl = "http://localhost:5000/graphql";
    private const string SettingsFileSuffix = "-settings.json";
    private const string DefaultSettingsFileName = "schema" + SettingsFileSuffix;
    private const string NamePropertyName = "name";
    private const string TransportsPropertyName = "transports";
    private const string HttpPropertyName = "http";
    private const string CapabilitiesPropertyName = "capabilities";
    private const string BatchingPropertyName = "batching";
    private const string ExtensionsPropertyName = "extensions";
    private const string ChilliCreamPropertyName = "chillicream";
    private const string ApolloFederationSupportPropertyName = "apolloFederationSupport";

    private static readonly JsonWriterOptions s_writerOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public FusionSourceSchemaInitCommand() : base("init")
    {
        Description = "Create a source schema settings file.";

        Options.Add(Opt<OptionalSourceSchemaNameOption>.Instance);
        Options.Add(Opt<OptionalSourceSchemaFileOption>.Instance);
        Options.Add(Opt<OptionalSettingsFileOption>.Instance);
        Options.Add(Opt<OptionalTransportUrlOption>.Instance);
        Options.Add(Opt<OptionalTransportDevUrlOption>.Instance);
        Options.Add(Opt<OptionalTransportClientNameOption>.Instance);
        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<SourceSchemaTypeOption>.Instance);
        Options.Add(Opt<OptionalVariableBatchingOption>.Instance);
        Options.Add(Opt<OptionalRequestBatchingOption>.Instance);
        Options.Add(Opt<OptionalAliasBatchingOption>.Instance);
        Options.Add(Opt<OptionalBatchingFormatListOption>.Instance);
        Options.Add(Opt<WorkingDirectoryOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            fusion source-schema init \
              --name "products" \
              --source-schema-file ./products/schema.graphqls \
              --url "https://products.example.com/graphql"
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

        var workingDirectory = parseResult.GetValue(Opt<WorkingDirectoryOption>.Instance)
            ?? fileSystem.GetCurrentDirectory();
        var settingsFile = ResolveSettingsFile(fileSystem, parseResult, workingDirectory);
        var settings = await TryReadSettingsAsync(fileSystem, settingsFile, cancellationToken);
        var exists = settings is not null;

        // an existing file already carries a name and a URL and keeps them unless they are passed.
        var name = exists
            ? parseResult.GetValue(Opt<OptionalSourceSchemaNameOption>.Instance)
            : await console.PromptAsync(
                "Source schema name",
                DeriveDefaultName(settingsFile),
                parseResult,
                Opt<OptionalSourceSchemaNameOption>.Instance,
                cancellationToken);

        var url = exists
            ? parseResult.GetValue(Opt<OptionalTransportUrlOption>.Instance)
            : await console.PromptAsync(
                "Source schema URL",
                DefaultUrl,
                parseResult,
                Opt<OptionalTransportUrlOption>.Instance,
                cancellationToken);

        // a prompted URL never passed through the option validator.
        if (url is not null && !TransportUrlOption.IsValid(url))
        {
            throw new ExitException(
                Messages.TransportUrlInvalid(OptionalTransportUrlOption.OptionName));
        }

        var schemaType = await ResolveSchemaTypeAsync(
            console,
            parseResult,
            exists,
            cancellationToken);

        var batching = await ResolveBatchingSettingsAsync(
            console,
            parseResult,
            exists,
            cancellationToken);

        settings ??= new JsonObject();

        ApplySettings(
            settings,
            name,
            url,
            parseResult.GetValue(Opt<OptionalTransportDevUrlOption>.Instance),
            parseResult.GetValue(Opt<OptionalTransportClientNameOption>.Instance),
            parseResult.GetValue(Opt<OptionalApiIdOption>.Instance),
            schemaType,
            batching.VariableBatching,
            batching.RequestBatching,
            batching.AliasBatching,
            batching.Formats,
            applyBatchingDefaults: !exists);

        // composition rejects a settings file without a name, so an existing file that never had
        // one is not silently written back in the same broken state.
        if (settings[NamePropertyName] is not JsonValue nameValue
            || !nameValue.TryGetValue<string>(out var appliedName)
            || string.IsNullOrWhiteSpace(appliedName))
        {
            throw new ExitException(Messages.SourceSchemaSettingsNameInvalid(settingsFile));
        }

        await WriteSettingsAsync(fileSystem, settingsFile, settings, cancellationToken);

        console.Success(
            exists
                ? $"Updated '{settingsFile.EscapeMarkup()}'."
                : $"Created '{settingsFile.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }

    private static bool? GetBooleanOption(ParseResult parseResult, Option<string> option)
        => parseResult.GetValue(option) is { } value ? bool.Parse(value) : null;

    private static async Task<BatchingSettings> ResolveBatchingSettingsAsync(
        INitroConsole console,
        ParseResult parseResult,
        bool settingsExist,
        CancellationToken cancellationToken)
    {
        var variableBatching =
            GetBooleanOption(parseResult, Opt<OptionalVariableBatchingOption>.Instance);
        var requestBatching =
            GetBooleanOption(parseResult, Opt<OptionalRequestBatchingOption>.Instance);
        var aliasBatching =
            GetBooleanOption(parseResult, Opt<OptionalAliasBatchingOption>.Instance);
        IReadOnlyList<string>? formats =
            parseResult.GetResult(Opt<OptionalBatchingFormatListOption>.Instance)
                is { Implicit: false }
                ? parseResult.GetValue(Opt<OptionalBatchingFormatListOption>.Instance)
                : null;

        if (settingsExist || !console.IsInteractive)
        {
            return new(variableBatching, requestBatching, aliasBatching, formats);
        }

        variableBatching ??= await console.ConfirmAsync(
            "Variable batching",
            defaultValue: false,
            cancellationToken);
        requestBatching ??= await console.ConfirmAsync(
            "Request batching",
            defaultValue: false,
            cancellationToken);
        aliasBatching ??= await console.ConfirmAsync(
            "Alias batching",
            defaultValue: true,
            cancellationToken);

        return new(variableBatching, requestBatching, aliasBatching, formats);
    }

    private static async Task<string?> ResolveSchemaTypeAsync(
        INitroConsole console,
        ParseResult parseResult,
        bool settingsExist,
        CancellationToken cancellationToken)
    {
        var schemaType = parseResult.GetValue(Opt<SourceSchemaTypeOption>.Instance);

        if (schemaType is not null || settingsExist)
        {
            return schemaType;
        }

        if (!console.IsInteractive)
        {
            return SourceSchemaTypeOption.GraphQLFederation;
        }

        var selected = await console.PromptAsync(
            "Source schema type",
            ["GraphQL Federation", "Apollo Federation 1", "Apollo Federation 2"],
            cancellationToken);

        return selected switch
        {
            "Apollo Federation 1" => SourceSchemaTypeOption.ApolloFederation1,
            "Apollo Federation 2" => SourceSchemaTypeOption.ApolloFederation2,
            _ => SourceSchemaTypeOption.GraphQLFederation
        };
    }

    /// <summary>
    /// Determines the settings file that composition expects for the targeted source schema.
    /// </summary>
    private static string ResolveSettingsFile(
        IFileSystem fileSystem,
        ParseResult parseResult,
        string workingDirectory)
    {
        var settingsFile = parseResult.GetValue(Opt<OptionalSettingsFileOption>.Instance);

        if (settingsFile is not null)
        {
            return Resolve(settingsFile);
        }

        var sourceSchemaFile = parseResult.GetValue(Opt<OptionalSourceSchemaFileOption>.Instance);

        if (sourceSchemaFile is null)
        {
            return Path.Combine(workingDirectory, DefaultSettingsFileName);
        }

        var sourceSchemaPath = Resolve(sourceSchemaFile);

        if (fileSystem.DirectoryExists(sourceSchemaPath))
        {
            // must stay in sync with the lookup in FusionCompositionHelpers.ReadSourceSchemaAsync,
            // otherwise composition looks for the settings file elsewhere.
            var schemaFile = fileSystem
                .GetFiles(sourceSchemaPath, "*.graphql*", SearchOption.AllDirectories)
                .FirstOrDefault(f =>
                {
                    var fileName = Path.GetFileName(f);
                    return FusionCompositionHelpers.IsSchemaFile(fileName)
                        && !FusionCompositionHelpers.IsExtensionsFile(fileName);
                });

            return schemaFile is null
                ? Path.Combine(sourceSchemaPath, DefaultSettingsFileName)
                : GetSettingsFile(schemaFile);
        }

        var sourceSchemaFileName = Path.GetFileName(sourceSchemaPath);

        if (FusionCompositionHelpers.IsExtensionsFile(sourceSchemaFileName))
        {
            throw new ExitException(
                Messages.SchemaExtensionsFileCannotBeUsedAsSchemaFile(sourceSchemaPath));
        }

        // a path that is not named like a schema file is a directory that does not exist yet.
        if (!FusionCompositionHelpers.IsSchemaFile(sourceSchemaFileName))
        {
            return Path.Combine(sourceSchemaPath, DefaultSettingsFileName);
        }

        return GetSettingsFile(sourceSchemaPath);

        string Resolve(string path)
            => Path.IsPathRooted(path) ? path : Path.Combine(workingDirectory, path);

        static string GetSettingsFile(string schemaFile)
            => Path.Combine(
                Path.GetDirectoryName(schemaFile)!,
                Path.GetFileNameWithoutExtension(schemaFile) + SettingsFileSuffix);
    }

    private static string? DeriveDefaultName(string settingsFile)
    {
        var directory = Path.GetDirectoryName(settingsFile);

        return string.IsNullOrEmpty(directory) ? null : Path.GetFileName(directory);
    }

    /// <summary>
    /// Reads the settings file, or returns <c>null</c> when it does not exist.
    /// </summary>
    private static async Task<JsonObject?> TryReadSettingsAsync(
        IFileSystem fileSystem,
        string settingsFile,
        CancellationToken cancellationToken)
    {
        if (!fileSystem.FileExists(settingsFile))
        {
            return null;
        }

        JsonNode? node;

        try
        {
            node = JsonNode.Parse(
                await fileSystem.ReadAllBytesAsync(settingsFile, cancellationToken));
        }
        catch (JsonException)
        {
            throw new ExitException(Messages.SourceSchemaSettingsFileNotAnObject(settingsFile));
        }

        if (node is not JsonObject settings)
        {
            throw new ExitException(Messages.SourceSchemaSettingsFileNotAnObject(settingsFile));
        }

        return settings;
    }

    /// <summary>
    /// Applies the values that were passed as options. A value that was not passed keeps whatever
    /// the settings already hold, except when new-file defaults are requested.
    /// </summary>
    private static void ApplySettings(
        JsonObject settings,
        string? name,
        string? url,
        string? devUrl,
        string? clientName,
        string? apiId,
        string? schemaType,
        bool? variableBatching,
        bool? requestBatching,
        bool? aliasBatching,
        IReadOnlyList<string>? batchingFormats,
        bool applyBatchingDefaults)
    {
        if (name is not null)
        {
            settings[NamePropertyName] = name;
        }

        if (url is not null || devUrl is not null || clientName is not null)
        {
            var http = GetOrAddObject(
                GetOrAddObject(settings, TransportsPropertyName),
                HttpPropertyName);

            if (url is not null)
            {
                http["url"] = url;
            }

            if (devUrl is not null)
            {
                http["devUrl"] = devUrl;
            }

            if (clientName is not null)
            {
                http["clientName"] = clientName;
            }
        }

        if (applyBatchingDefaults
            || variableBatching is not null
            || requestBatching is not null
            || aliasBatching is not null
            || batchingFormats is not null)
        {
            var batching = GetOrAddObject(
                GetOrAddObject(
                    GetOrAddObject(
                        GetOrAddObject(settings, TransportsPropertyName),
                        HttpPropertyName),
                    CapabilitiesPropertyName),
                BatchingPropertyName);

            if (variableBatching is not null || applyBatchingDefaults)
            {
                batching["variableBatching"] = variableBatching ?? false;
            }

            if (requestBatching is not null || applyBatchingDefaults)
            {
                batching["requestBatching"] = requestBatching ?? false;
            }

            if (aliasBatching is not null || applyBatchingDefaults)
            {
                batching["aliasBatching"] = aliasBatching ?? true;
            }

            if (batchingFormats is not null)
            {
                batching["formats"] = new JsonArray(
                    batchingFormats.Select(static format => JsonValue.Create(format)).ToArray());
            }
        }

        if (apiId is not null)
        {
            GetOrAddObject(GetOrAddObject(settings, ExtensionsPropertyName), "nitro")["apiId"] =
                apiId;
        }

        if (schemaType is SourceSchemaTypeOption.ApolloFederation1
            or SourceSchemaTypeOption.ApolloFederation2)
        {
            var version = schemaType is SourceSchemaTypeOption.ApolloFederation1
                ? "1.0"
                : "2.0";

            GetOrAddObject(GetOrAddObject(settings, ExtensionsPropertyName), ChilliCreamPropertyName)
                [ApolloFederationSupportPropertyName] = new JsonObject { ["version"] = version };
        }
        else if (schemaType is SourceSchemaTypeOption.GraphQLFederation)
        {
            RemoveApolloFederationSupport(settings);
        }
    }

    private static void RemoveApolloFederationSupport(JsonObject settings)
    {
        if (settings[ExtensionsPropertyName] is not JsonObject extensions
            || extensions[ChilliCreamPropertyName] is not JsonObject chilliCream)
        {
            return;
        }

        chilliCream.Remove(ApolloFederationSupportPropertyName);

        if (chilliCream.Count == 0)
        {
            extensions.Remove(ChilliCreamPropertyName);
        }

        if (extensions.Count == 0)
        {
            settings.Remove(ExtensionsPropertyName);
        }
    }

    private static JsonObject GetOrAddObject(JsonObject parent, string propertyName)
    {
        if (parent[propertyName] is JsonObject existing)
        {
            return existing;
        }

        var child = new JsonObject();
        parent[propertyName] = child;

        return child;
    }

    private static async Task WriteSettingsAsync(
        IFileSystem fileSystem,
        string settingsFile,
        JsonObject settings,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(settingsFile);

        if (!string.IsNullOrEmpty(directory) && !fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }

        await using var stream = fileSystem.CreateFile(settingsFile);
        await using var writer = new Utf8JsonWriter(stream, s_writerOptions);

        settings.WriteTo(writer);
        await writer.FlushAsync(cancellationToken);
    }

    private readonly record struct BatchingSettings(
        bool? VariableBatching,
        bool? RequestBatching,
        bool? AliasBatching,
        IReadOnlyList<string>? Formats);
}
