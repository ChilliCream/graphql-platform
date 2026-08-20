using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Search;

/// <summary>
/// A parse failure on a search-mode query line: an unknown key: prefix, or
/// an invalid value for a recognized key. <see cref="Position"/> is the
/// zero-based index of the failing token in the input string.
/// </summary>
internal readonly record struct TaskQueryParseError(string Message, int Position);

/// <summary>
/// Parses one search-mode query line into a <see cref="TaskQuery"/>.
/// Whitespace-separated tokens: bare words accumulate into free text;
/// key:value tokens map to filter fields; quoting a token with double
/// quotes keeps embedded spaces and disables keyword and key: interpretation
/// for that token. Pure: no I/O, no wall-clock reads, no exceptions thrown
/// for malformed input.
/// </summary>
internal static class TaskQueryParser
{
    private const string ReadyKeyword = "ready";
    private const string BlockedKeyword = "blocked";
    private const string StaleKeyword = "stale";

    private const string StatusKey = "status";
    private const string TypeKey = "type";
    private const string PriorityKey = "priority";
    private const string LabelKey = "label";
    private const string AssigneeKey = "assignee";

    /// <summary>
    /// Parses <paramref name="input"/>. Returns true with <paramref name="query"/>
    /// populated on success. Returns false with <paramref name="error"/>
    /// populated when a token has an unrecognized key: prefix or an invalid
    /// value for a recognized key.
    /// </summary>
    public static bool TryParse(
        string input,
        out TaskQuery query,
        out TaskQueryParseError error)
    {
        List<string>? text = null;
        List<string>? statuses = null;
        List<string>? labels = null;
        string? type = null;
        int? priority = null;
        string? assignee = null;
        var ready = false;
        var blocked = false;
        var stale = false;

        foreach (var (raw, position) in Tokenize(input))
        {
            if (TrySplitKeyValue(raw, out var key, out var value))
            {
                switch (key.ToLowerInvariant())
                {
                    case StatusKey:
                        (statuses ??= []).Add(TaskStates.Normalize(value));
                        break;

                    case TypeKey:
                        type = TaskTypes.Normalize(value);
                        break;

                    case PriorityKey:
                        if (!TryParsePriority(value, out var priorityFromKey))
                        {
                            query = TaskQuery.Empty;
                            error = new TaskQueryParseError(
                                $"Invalid priority '{value}'. Use 0-4 or p0-p4 "
                                + "(0 = critical, 4 = backlog).",
                                position);
                            return false;
                        }

                        priority = priorityFromKey;
                        break;

                    case LabelKey:
                        (labels ??= []).Add(value.Trim().ToLowerInvariant());
                        break;

                    case AssigneeKey:
                        assignee = value;
                        break;

                    default:
                        query = TaskQuery.Empty;
                        error = new TaskQueryParseError($"Unknown key '{key}:'.", position);
                        return false;
                }

                continue;
            }

            var isQuoted = raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"';
            var word = Unquote(raw);

            if (!isQuoted && string.Equals(word, ReadyKeyword, StringComparison.OrdinalIgnoreCase))
            {
                ready = true;
            }
            else if (!isQuoted
                && string.Equals(word, BlockedKeyword, StringComparison.OrdinalIgnoreCase))
            {
                blocked = true;
            }
            else if (!isQuoted
                && string.Equals(word, StaleKeyword, StringComparison.OrdinalIgnoreCase))
            {
                stale = true;
            }
            else if (!isQuoted && TryParseBarePriority(word, out var priorityFromWord))
            {
                priority = priorityFromWord;
            }
            else
            {
                (text ??= []).Add(word);
            }
        }

        query = new TaskQuery
        {
            Text = text is null ? null : string.Join(' ', text),
            Statuses = statuses,
            Type = type,
            Priority = priority,
            Assignee = assignee,
            Labels = labels,
            Ready = ready,
            Blocked = blocked,
            Stale = stale
        };
        error = default;
        return true;
    }

    /// <summary>
    /// Splits a whitespace-delimited raw token into a key and value at its
    /// first unquoted colon. Returns false when the token has no such
    /// colon, so the caller treats it as a bare word.
    /// </summary>
    private static bool TrySplitKeyValue(string raw, out string key, out string value)
    {
        if (raw.Length == 0 || raw[0] == '"')
        {
            key = "";
            value = "";
            return false;
        }

        var quoteIndex = raw.IndexOf('"');
        var searchLimit = quoteIndex >= 0 ? quoteIndex : raw.Length;
        var colonIndex = raw.IndexOf(':', 0, searchLimit);

        if (colonIndex <= 0)
        {
            key = "";
            value = "";
            return false;
        }

        key = raw[..colonIndex];
        value = Unquote(raw[(colonIndex + 1)..]);
        return true;
    }

    /// <summary>
    /// Strips a single pair of matching double quotes wrapping the whole
    /// value, if present.
    /// </summary>
    private static string Unquote(string value)
        => value.Length >= 2 && value[0] == '"' && value[^1] == '"'
            ? value[1..^1]
            : value;

    /// <summary>
    /// Parses "0".."4" or "p0".."p4" (case-insensitive), mirroring
    /// <see cref="TaskPriorities.Parse"/> without throwing.
    /// </summary>
    private static bool TryParsePriority(string value, out int priority)
    {
        var span = value.AsSpan().Trim();

        if (span.Length > 1 && span[0] is 'p' or 'P')
        {
            span = span[1..];
        }

        if (span.Length == 1 && span[0] is >= '0' and <= '4')
        {
            priority = span[0] - '0';
            return true;
        }

        priority = 0;
        return false;
    }

    /// <summary>
    /// Matches a bare "p0".."p4" token (case-insensitive), the standalone
    /// keyword form of a priority filter.
    /// </summary>
    private static bool TryParseBarePriority(string value, out int priority)
    {
        if (value.Length == 2 && value[0] is 'p' or 'P' && value[1] is >= '0' and <= '4')
        {
            priority = value[1] - '0';
            return true;
        }

        priority = 0;
        return false;
    }

    /// <summary>
    /// Splits input into whitespace-delimited tokens, treating a double-quoted
    /// span as part of the enclosing token even when it contains whitespace.
    /// Each token is paired with its zero-based start index in the input.
    /// </summary>
    private static IEnumerable<(string Value, int Position)> Tokenize(string input)
    {
        var i = 0;
        var length = input.Length;

        while (i < length)
        {
            while (i < length && char.IsWhiteSpace(input[i]))
            {
                i++;
            }

            if (i >= length)
            {
                yield break;
            }

            var start = i;

            while (i < length && !char.IsWhiteSpace(input[i]))
            {
                if (input[i] == '"')
                {
                    i++;

                    while (i < length && input[i] != '"')
                    {
                        i++;
                    }

                    if (i < length)
                    {
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }

            yield return (input[start..i], start);
        }
    }
}
