using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Parses the restricted frontmatter grammar memory markdown files use: a
/// fixed, non-nested set of <c>key: value</c> lines between two <c>---</c>
/// delimiters, followed by the markdown body. This is deliberately not a
/// YAML parser; the grammar accepts nothing YAML allows beyond what is
/// spelled out here, so there is no ambiguity to resolve and no YAML
/// package dependency to take on.
/// </summary>
/// <remarks>
/// The failure contract is strict by design: an unsupported schema version,
/// malformed frontmatter, an unknown key, or a filename/id mismatch all
/// fail parsing rather than tolerating or silently dropping the offending
/// data. Callers that build an index from many files must not let a failed
/// parse replace a previously valid index entry.
/// </remarks>
internal static class MemoryFrontmatterParser
{
    public const int SupportedSchemaVersion = 1;

    private const string Delimiter = "---";

    private static readonly string[] s_requiredKeys =
        ["schema", "id", "type", "tags", "created_at", "updated_at", "created_by"];

    private static readonly HashSet<string> s_knownKeys =
        new(s_requiredKeys) { "promoted_from" };

    public static bool TryParse(
        string content,
        string expectedId,
        [NotNullWhen(true)] out MemoryFrontmatter? frontmatter,
        [NotNullWhen(false)] out MemoryFrontmatterFailure? failure)
    {
        frontmatter = null;
        failure = null;

        var lines = content.Replace("\r\n", "\n").Split('\n');

        if (lines.Length == 0 || lines[0] != Delimiter)
        {
            failure = Fail(MemoryFrontmatterFailureReason.MalformedFrontmatter,
                "Frontmatter must start with a '---' delimiter line.");
            return false;
        }

        var closeIndex = -1;

        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i] == Delimiter)
            {
                closeIndex = i;
                break;
            }
        }

        if (closeIndex < 0)
        {
            failure = Fail(MemoryFrontmatterFailureReason.MalformedFrontmatter,
                "Frontmatter is missing its closing '---' delimiter line.");
            return false;
        }

        var fields = new Dictionary<string, string>();

        for (var i = 1; i < closeIndex; i++)
        {
            var line = lines[i];
            var separator = line.IndexOf(':');

            if (separator <= 0)
            {
                failure = Fail(MemoryFrontmatterFailureReason.MalformedFrontmatter,
                    $"Line '{line}' is not a 'key: value' pair.");
                return false;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            if (key.Length == 0 || key.Any(char.IsWhiteSpace))
            {
                failure = Fail(MemoryFrontmatterFailureReason.MalformedFrontmatter,
                    $"Line '{line}' has an invalid key.");
                return false;
            }

            if (!s_knownKeys.Contains(key))
            {
                failure = Fail(MemoryFrontmatterFailureReason.UnknownKey,
                    $"Unknown frontmatter key '{key}'.");
                return false;
            }

            if (!fields.TryAdd(key, value))
            {
                failure = Fail(MemoryFrontmatterFailureReason.MalformedFrontmatter,
                    $"Frontmatter key '{key}' appears more than once.");
                return false;
            }
        }

        foreach (var requiredKey in s_requiredKeys)
        {
            if (!fields.ContainsKey(requiredKey))
            {
                failure = Fail(MemoryFrontmatterFailureReason.MissingRequiredKey,
                    $"Frontmatter is missing required key '{requiredKey}'.");
                return false;
            }
        }

        if (!int.TryParse(fields["schema"], NumberStyles.None, CultureInfo.InvariantCulture, out var schema)
            || schema != SupportedSchemaVersion)
        {
            failure = Fail(MemoryFrontmatterFailureReason.UnsupportedSchemaVersion,
                $"Unsupported frontmatter schema version '{fields["schema"]}'.");
            return false;
        }

        var id = fields["id"];

        if (id.Length == 0)
        {
            failure = Fail(MemoryFrontmatterFailureReason.InvalidValue, "The 'id' value is empty.");
            return false;
        }

        if (id != expectedId)
        {
            failure = Fail(MemoryFrontmatterFailureReason.FilenameIdMismatch,
                $"Frontmatter id '{id}' does not match the filename id '{expectedId}'.");
            return false;
        }

        var normalizedType = MemoryTypes.Normalize(fields["type"]);

        if (!MemoryTypes.IsValid(normalizedType))
        {
            failure = Fail(MemoryFrontmatterFailureReason.InvalidValue,
                $"The 'type' value '{fields["type"]}' is invalid.");
            return false;
        }

        if (!TryParseTags(fields["tags"], out var tags))
        {
            failure = Fail(MemoryFrontmatterFailureReason.InvalidValue,
                $"The 'tags' value '{fields["tags"]}' is invalid; expected '[]' or '[tag, tag]'.");
            return false;
        }

        if (!TryParseTimestamp(fields["created_at"], out var createdAt))
        {
            failure = Fail(MemoryFrontmatterFailureReason.InvalidValue,
                $"The 'created_at' value '{fields["created_at"]}' is not a UTC RFC 3339 timestamp.");
            return false;
        }

        if (!TryParseTimestamp(fields["updated_at"], out var updatedAt))
        {
            failure = Fail(MemoryFrontmatterFailureReason.InvalidValue,
                $"The 'updated_at' value '{fields["updated_at"]}' is not a UTC RFC 3339 timestamp.");
            return false;
        }

        var createdBy = fields["created_by"];

        if (createdBy.Length == 0)
        {
            failure = Fail(MemoryFrontmatterFailureReason.InvalidValue, "The 'created_by' value is empty.");
            return false;
        }

        string? promotedFrom = null;

        if (fields.TryGetValue("promoted_from", out var promotedFromValue))
        {
            if (promotedFromValue.Length == 0)
            {
                failure = Fail(MemoryFrontmatterFailureReason.InvalidValue,
                    "The 'promoted_from' value is empty.");
                return false;
            }

            promotedFrom = promotedFromValue;
        }

        var body = closeIndex + 1 < lines.Length
            ? string.Join('\n', lines[(closeIndex + 1)..]).TrimStart('\n')
            : string.Empty;

        frontmatter = new MemoryFrontmatter(
            schema, id, normalizedType, tags, createdAt, updatedAt, createdBy, promotedFrom, body);
        return true;
    }

    private static bool TryParseTags(string value, out IReadOnlyList<string> tags)
    {
        tags = [];

        if (value.Length < 2 || value[0] != '[' || value[^1] != ']')
        {
            return false;
        }

        var inner = value[1..^1].Trim();

        if (inner.Length == 0)
        {
            tags = [];
            return true;
        }

        var parsed = new List<string>();

        foreach (var rawTag in inner.Split(','))
        {
            var normalized = MemoryTags.Normalize(rawTag);

            if (!MemoryTags.IsValid(normalized))
            {
                return false;
            }

            parsed.Add(normalized);
        }

        tags = parsed;
        return true;
    }

    private static bool TryParseTimestamp(string value, out DateTimeOffset timestamp)
    {
        // Stored timestamps must be UTC RFC 3339, marked with the 'Z'
        // designator; frontmatter is machine-written, not user-typed, so
        // this is stricter than the offset-inferring parsing options use.
        if (value.EndsWith('Z')
            && DateTimeOffset.TryParse(
                value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out timestamp))
        {
            return true;
        }

        timestamp = default;
        return false;
    }

    private static MemoryFrontmatterFailure Fail(MemoryFrontmatterFailureReason reason, string message)
        => new(reason, message);
}
