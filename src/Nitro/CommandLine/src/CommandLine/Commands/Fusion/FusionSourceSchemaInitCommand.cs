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
        Options.Add(Opt<SourceSchemaKindOption>.Instance);
        Options.Add(Opt<OptionalApolloFederationVersionOption>.Instance);
        Options.Add(Opt<WorkingDirectoryOption>.Instance);

        this.AddGlobalNitroOptions();

        Validators.Add(result =>
        {
            var kind = result.GetValue(Opt<SourceSchemaKindOption>.Instance);
            var version = result.GetValue(Opt<OptionalApolloFederationVersionOption>.Instance);

            if (version is not null && kind != SourceSchemaKindOption.ApolloFederation)
            {
                result.AddError(Messages.ApolloFederationVersionRequiresKind());
            }
        });

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

        // a new settings file needs a name and a URL, so both are asked for when they are not
        // passed as options. An existing file already carries them and keeps its values.
        var name = exists
            ? parseResult.GetValue(Opt<OptionalSourceSchemaNameOption>.Instance)
            : await console.PromptAsync(
                "Source schema name",
                DeriveDefaultName(settingsFile),
                parseResult,
                Opt<OptionalSourceSchemaNameOption>.Instance,
                cancellationToken);

        var url = parseResult.GetValue(Opt<OptionalTransportUrlOption>.Instance);

        if (url is null && !exists)
        {
            url = console.IsInteractive
                ? await console.PromptAsync("Source schema URL", DefaultUrl, cancellationToken)
                : DefaultUrl;
        }

        settings ??= new JsonObject();

        ApplySettings(
            settings,
            name,
            url,
            parseResult.GetValue(Opt<OptionalTransportDevUrlOption>.Instance),
            parseResult.GetValue(Opt<OptionalTransportClientNameOption>.Instance),
            parseResult.GetValue(Opt<OptionalApiIdOption>.Instance),
            parseResult.GetValue(Opt<SourceSchemaKindOption>.Instance) ?? SourceSchemaKindOption.Generic,
            parseResult.GetValue(Opt<OptionalApolloFederationVersionOption>.Instance));

        await WriteSettingsAsync(fileSystem, settingsFile, settings, cancellationToken);

        console.Success(
            exists
                ? $"Updated '{settingsFile.EscapeMarkup()}'."
                : $"Created '{settingsFile.EscapeMarkup()}'.");

        return ExitCodes.Success;
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
            // the same lookup composition performs, so the settings file lands next to the
            // schema file that composition picks up.
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

        if (FusionCompositionHelpers.IsExtensionsFile(Path.GetFileName(sourceSchemaPath)))
        {
            throw new ExitException(
                Messages.SchemaExtensionsFileCannotBeUsedAsSchemaFile(sourceSchemaPath));
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
    /// Reads the settings file when it exists, so that an existing file keeps every setting that
    /// the command does not touch.
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

    private static void ApplySettings(
        JsonObject settings,
        string? name,
        string? url,
        string? devUrl,
        string? clientName,
        string? apiId,
        string kind,
        string? apolloFederationVersion)
    {
        if (name is not null)
        {
            settings["name"] = name;
        }

        var isHotChocolate = kind is SourceSchemaKindOption.HotChocolate;

        if (url is not null || devUrl is not null || clientName is not null || isHotChocolate)
        {
            var http = GetOrAddObject(GetOrAddObject(settings, "transports"), "http");

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

            if (isHotChocolate)
            {
                // a Hot Chocolate source schema implements these transport extensions, so the
                // gateway is told about them instead of being left on the defaults.
                var capabilities = GetOrAddObject(http, "capabilities");
                var batching = GetOrAddObject(capabilities, "batching");

                batching["variableBatching"] = true;
                batching["requestBatching"] = true;
                batching["aliasBatching"] = true;
                capabilities["onError"] = "propagate";
            }
        }

        if (apiId is not null)
        {
            GetOrAddObject(GetOrAddObject(settings, "extensions"), "nitro")["apiId"] = apiId;
        }

        if (kind is SourceSchemaKindOption.ApolloFederation)
        {
            var chilliCream = GetOrAddObject(GetOrAddObject(settings, "extensions"), "chillicream");

            GetOrAddObject(chilliCream, "apolloFederationSupport")["version"] =
                apolloFederationVersion ?? OptionalApolloFederationVersionOption.Version2;
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
}
