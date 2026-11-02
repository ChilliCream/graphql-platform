#!/usr/bin/env dotnet

#:project ../../src/HotChocolate/Language/src/Language.Utf8/HotChocolate.Language.Utf8.csproj

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;

const string DefaultSourceDirectory =
    "src/HotChocolate/Fusion/src/Fusion.Aspire";

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintUsage();
    return args.Length == 0 ? 1 : 0;
}

var command = args[0];
var sourceDirectory = DefaultSourceDirectory;
string? outputPath = null;

for (var i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--source" when i + 1 < args.Length:
            sourceDirectory = args[++i];
            break;
        case "--output" when i + 1 < args.Length:
            outputPath = args[++i];
            break;
        default:
            Console.Error.WriteLine($"Unknown or incomplete argument: {args[i]}");
            PrintUsage();
            return 1;
    }
}

sourceDirectory = Path.GetFullPath(sourceDirectory);
if (!Directory.Exists(sourceDirectory))
{
    Console.Error.WriteLine($"Source directory does not exist: {sourceDirectory}");
    return 1;
}

var queryPaths = Directory
    .EnumerateFiles(sourceDirectory, "*.graphql", SearchOption.AllDirectories)
    .OrderBy(path => path, StringComparer.Ordinal)
    .ToArray();

if (queryPaths.Length == 0)
{
    Console.Error.WriteLine($"No .graphql files found under: {sourceDirectory}");
    return 1;
}

if (command == "update")
{
    foreach (var queryPath in queryPaths)
    {
        var document = NormalizeDocument(File.ReadAllText(queryPath));
        var hash = ComputeSha256(document);
        var sidecarPath = GetSidecarPath(queryPath);
        File.WriteAllText(sidecarPath, hash + "\n", new UTF8Encoding(false));
        Console.WriteLine($"Updated {Path.GetRelativePath(sourceDirectory, sidecarPath)}");
    }

    return 0;
}

if (command != "verify")
{
    Console.Error.WriteLine($"Unknown command: {command}");
    PrintUsage();
    return 1;
}

if (string.IsNullOrWhiteSpace(outputPath))
{
    Console.Error.WriteLine("The verify command requires --output <path>.");
    return 1;
}

var operations = new SortedDictionary<string, string>(StringComparer.Ordinal);
var failed = false;

foreach (var queryPath in queryPaths)
{
    var document = NormalizeDocument(File.ReadAllText(queryPath));
    var actualHash = ComputeSha256(document);
    var sidecarPath = GetSidecarPath(queryPath);
    var relativeQueryPath = Path.GetRelativePath(sourceDirectory, queryPath);

    if (!File.Exists(sidecarPath))
    {
        Console.Error.WriteLine(
            $"Missing SHA-256 sidecar for {relativeQueryPath}: " +
            $"{Path.GetFileName(sidecarPath)}");
        failed = true;
        continue;
    }

    var expectedHash = File.ReadAllText(sidecarPath).Trim();
    if (expectedHash.Length != 64
        || expectedHash.Any(c => c is not (>= '0' and <= '9')
            and not (>= 'a' and <= 'f')))
    {
        Console.Error.WriteLine(
            $"Invalid SHA-256 sidecar for {relativeQueryPath}: expected 64 lowercase hex characters.");
        failed = true;
        continue;
    }

    if (!string.Equals(expectedHash, actualHash, StringComparison.Ordinal))
    {
        Console.Error.WriteLine(
            $"SHA-256 mismatch for {relativeQueryPath}: expected {expectedHash}, actual {actualHash}.");
        failed = true;
        continue;
    }

    if (operations.TryGetValue(actualHash, out var existingDocument)
        && !string.Equals(existingDocument, document, StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"SHA-256 collision detected for {relativeQueryPath}.");
        failed = true;
        continue;
    }

    operations[actualHash] = document;
}

if (failed)
{
    Console.Error.WriteLine(
        "Persisted operation verification failed. Run " +
        "'.github/scripts/nitro-aspire-operations.sh update' and commit the sidecars.");
    return 1;
}

outputPath = Path.GetFullPath(outputPath);
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

using (var stream = File.Create(outputPath))
{
    using var writer = new Utf8JsonWriter(stream);
    writer.WriteStartObject();
    foreach (var (hash, document) in operations)
    {
        writer.WriteString(hash, document);
    }
    writer.WriteEndObject();
}

File.AppendAllText(outputPath, "\n", new UTF8Encoding(false));

Console.WriteLine(
    $"Verified {operations.Count} persisted operation(s) and wrote {outputPath}.");
return 0;

static string GetSidecarPath(string queryPath) => queryPath + ".sha256";

static string NormalizeDocument(string source)
    => Utf8GraphQLParser.Parse(source).ToString(false);

static string ComputeSha256(string document)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(document));
    return Convert.ToHexStringLower(bytes);
}

static void PrintUsage()
{
    Console.Error.WriteLine(
        """
        usage:
          nitro-aspire-operations.sh update [--source <directory>]
          nitro-aspire-operations.sh verify --output <operations.json> [--source <directory>]

        update writes a <query>.graphql.sha256 sidecar for every GraphQL document.
        verify checks every sidecar and writes a deterministic Nitro operations JSON file.
        """);
}
