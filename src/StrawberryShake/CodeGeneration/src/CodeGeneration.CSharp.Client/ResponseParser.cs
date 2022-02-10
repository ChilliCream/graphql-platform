using System.Text.Json;

namespace StrawberryShake.CodeGeneration.CSharp;

public static class ResponseParser
{
    public static GeneratorResponse Parse(string fileName)
    {
        var buffer = File.ReadAllBytes(fileName);
        var document = JsonDocument.Parse(buffer);
        File.Delete(fileName);
        return DeserializeGeneratorResponse(document.RootElement);
    }

    private static GeneratorResponse DeserializeGeneratorResponse(JsonElement root)
    {
        var documents = new List<GeneratorDocument>();
        var errors = new List<GeneratorError>();

        if (root.TryGetProperty("documents", out var prop))
        {
            foreach (var doc in prop.EnumerateArray())
            {
                documents.Add(DeserializeGeneratorDocument(doc));
            }
        }

        if (root.TryGetProperty("errors", out prop))
        {
            foreach (var doc in prop.EnumerateArray())
            {
                errors.Add(DeserializeGeneratorError(doc));
            }
        }

        return new GeneratorResponse(documents, errors);
    }

    private static GeneratorDocument DeserializeGeneratorDocument(JsonElement root)
    {
        string? hash = null;
        string? path = null;

        if (root.TryGetProperty("hash", out var prop))
        {
            hash = prop.GetString();
        }

        if (root.TryGetProperty("path", out prop))
        {
            path = prop.GetString();
        }

        return new GeneratorDocument(
            root.GetProperty("name").GetString()!,
            root.GetProperty("sourceText").GetString()!,
            (GeneratorDocumentKind)Enum.Parse(
                typeof(GeneratorDocumentKind),
                root.GetProperty("kind").GetString()!),
            hash,
            path);
    }

    private static GeneratorError DeserializeGeneratorError(JsonElement root)
    {
        string? filePath = null;
        Location? location = null;

        if (root.TryGetProperty("filePath", out var prop))
        {
            filePath = prop.GetString();
        }

        if (root.TryGetProperty("location", out prop))
        {
            location = DeserializeLocation(prop);
        }

        return new GeneratorError(
            root.GetProperty("code").GetString()!,
            root.GetProperty("title").GetString()!,
            root.GetProperty("message").GetString()!,
            filePath,
            location);
    }

    private static Location DeserializeLocation(JsonElement root)
    {
        return new Location(
            root.GetProperty("line").GetInt32(),
            root.GetProperty("column").GetInt32());
    }
}
