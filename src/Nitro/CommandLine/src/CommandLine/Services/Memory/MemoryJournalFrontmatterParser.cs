using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Parses the restricted frontmatter grammar journal entry markdown files
/// use: a fixed, non-nested set of <c>key: value</c> lines between two
/// <c>---</c> delimiters, followed by the markdown body. Mirrors
/// <see cref="MemoryFrontmatterParser"/>'s grammar and strict failure
/// contract, over the smaller key set a journal entry has: no type, tags,
/// updated-at timestamp, or promoted-from, since a journal entry is an
/// immutable capture rather than an editable curated memory.
/// </summary>
internal static class MemoryJournalFrontmatterParser
{
    public const int SupportedSchemaVersion = 1;

    private const string Delimiter = "---";

    private static readonly string[] s_requiredKeys = ["schema", "id", "created_at", "created_by"];
    private static readonly HashSet<string> s_knownKeys = new(s_requiredKeys);

    public static bool TryParse(
        string content,
        string expectedId,
        [NotNullWhen(true)] out MemoryJournalFrontmatter? frontmatter,
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

        if (!TryParseTimestamp(fields["created_at"], out var createdAt))
        {
            failure = Fail(MemoryFrontmatterFailureReason.InvalidValue,
                $"The 'created_at' value '{fields["created_at"]}' is not a UTC RFC 3339 timestamp.");
            return false;
        }

        var createdBy = fields["created_by"];

        if (createdBy.Length == 0)
        {
            failure = Fail(MemoryFrontmatterFailureReason.InvalidValue, "The 'created_by' value is empty.");
            return false;
        }

        var body = closeIndex + 1 < lines.Length
            ? string.Join('\n', lines[(closeIndex + 1)..]).TrimStart('\n')
            : string.Empty;

        frontmatter = new MemoryJournalFrontmatter(schema, id, createdAt, createdBy, body);
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
