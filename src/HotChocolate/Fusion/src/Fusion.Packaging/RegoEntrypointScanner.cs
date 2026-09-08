namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Scans a single Rego module for its declared entrypoint decisions: rules annotated
/// <c># METADATA</c> / <c>entrypoint: true</c>. Shared by the archive packaging tools (which use it
/// to validate a Rego policy bundle manifest against the modules it indexes) and the Rego policy
/// provider (which uses it to derive the decisions a compiled module exposes), so decision discovery
/// is defined in exactly one place.
/// </summary>
internal static class RegoEntrypointScanner
{
    public static List<string> Scan(string source)
    {
        var entryPoints = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var state = new LexicalState();
        var packageSeen = false;
        var metadataPending = false;
        var entrypointPending = false;

        foreach (var line in source.AsSpan().EnumerateLines())
        {
            var kind = ScanLine(line, ref state, out var content);

            if (kind is LineKind.Empty)
            {
                continue;
            }

            if (kind is LineKind.Comment)
            {
                if (!packageSeen)
                {
                    continue;
                }

                if (IsMetadataStart(content))
                {
                    metadataPending = true;
                    entrypointPending = false;
                }
                else if (metadataPending && IsEntrypointMetadata(content))
                {
                    entrypointPending = true;
                }

                continue;
            }

            if (!packageSeen && IsPackageDeclaration(content))
            {
                packageSeen = true;
                continue;
            }

            if (packageSeen && metadataPending)
            {
                if (entrypointPending && TryGetRuleName(content, out var name) && seen.Add(name))
                {
                    entryPoints.Add(name);
                }

                metadataPending = false;
                entrypointPending = false;
            }
        }

        return entryPoints;
    }

    private static LineKind ScanLine(
        ReadOnlySpan<char> line,
        ref LexicalState state,
        out ReadOnlySpan<char> content)
    {
        var contentStart = -1;
        var commentStart = -1;
        var startsAtTopLevel = state.IsTopLevel;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (state.InRawString)
            {
                if (c is '`')
                {
                    state.InRawString = false;
                }

                continue;
            }

            if (state.InString)
            {
                if (state.Escaped)
                {
                    state.Escaped = false;
                }
                else if (c is '\\')
                {
                    state.Escaped = true;
                }
                else if (c is '"')
                {
                    state.InString = false;
                }

                continue;
            }

            if (c is '#')
            {
                commentStart = i;
                break;
            }

            if (state.IsTopLevel && contentStart < 0 && !char.IsWhiteSpace(c))
            {
                contentStart = i;
            }

            switch (c)
            {
                case '"':
                    state.InString = true;
                    break;
                case '`':
                    state.InRawString = true;
                    break;
                case '{':
                    state.CurlyDepth++;
                    break;
                case '}':
                    state.CurlyDepth--;
                    break;
                case '[':
                    state.SquareDepth++;
                    break;
                case ']':
                    state.SquareDepth--;
                    break;
                case '(':
                    state.ParenthesisDepth++;
                    break;
                case ')':
                    state.ParenthesisDepth--;
                    break;
            }
        }

        if (contentStart < 0)
        {
            if (startsAtTopLevel && commentStart >= 0)
            {
                content = line[commentStart..].Trim();
                return LineKind.Comment;
            }

            content = default;
            return LineKind.Empty;
        }

        content = line[contentStart..(commentStart >= 0 ? commentStart : line.Length)].TrimEnd();
        return startsAtTopLevel ? LineKind.Code : LineKind.Empty;
    }

    private static bool IsMetadataStart(ReadOnlySpan<char> line)
        => line.SequenceEqual("# METADATA");

    private static bool IsEntrypointMetadata(ReadOnlySpan<char> line)
        => line[1..].TrimStart().SequenceEqual("entrypoint: true");

    private static bool IsPackageDeclaration(ReadOnlySpan<char> line)
        => StartsWithKeyword(line, "package");

    private static bool TryGetRuleName(ReadOnlySpan<char> line, out string name)
    {
        var isDefault = StartsWithKeyword(line, "default");

        if (isDefault)
        {
            line = line[7..].TrimStart();
        }

        var length = GetIdentifierLength(line);

        if (length == 0 || IsReservedKeyword(line[..length]))
        {
            name = string.Empty;
            return false;
        }

        var suffix = line[length..].TrimStart();

        if (!IsSimpleRuleSuffix(suffix, isDefault))
        {
            name = string.Empty;
            return false;
        }

        name = line[..length].ToString();
        return true;
    }

    private static bool IsSimpleRuleSuffix(ReadOnlySpan<char> suffix, bool isDefault)
    {
        if (isDefault)
        {
            return HasExpression(suffix, ":=") || HasExpression(suffix, "=");
        }

        if (suffix.StartsWith("{", StringComparison.Ordinal))
        {
            return true;
        }

        if (HasExpression(suffix, ":=") || HasExpression(suffix, "="))
        {
            return true;
        }

        return StartsWithKeyword(suffix, "if")
            && suffix[2..].TrimStart().StartsWith("{", StringComparison.Ordinal);
    }

    private static bool HasExpression(ReadOnlySpan<char> suffix, ReadOnlySpan<char> assignment)
        => suffix.StartsWith(assignment, StringComparison.Ordinal)
            && !suffix[assignment.Length..].Trim().IsEmpty;

    private static bool StartsWithKeyword(ReadOnlySpan<char> line, ReadOnlySpan<char> keyword)
        => line.StartsWith(keyword, StringComparison.Ordinal)
            && line.Length > keyword.Length
            && char.IsWhiteSpace(line[keyword.Length]);

    private static int GetIdentifierLength(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty || !IsIdentifierStart(value[0]))
        {
            return 0;
        }

        var length = 1;

        while (length < value.Length && IsIdentifierPart(value[length]))
        {
            length++;
        }

        return length;
    }

    private static bool IsIdentifierStart(char c)
        => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';

    private static bool IsIdentifierPart(char c)
        => IsIdentifierStart(c) || c is >= '0' and <= '9';

    private static bool IsReservedKeyword(ReadOnlySpan<char> value)
        => value.SequenceEqual("package")
            || value.SequenceEqual("import")
            || value.SequenceEqual("else")
            || value.SequenceEqual("not")
            || value.SequenceEqual("some")
            || value.SequenceEqual("with");

    private enum LineKind
    {
        Empty,
        Comment,
        Code
    }

    private struct LexicalState
    {
        public int CurlyDepth;
        public int SquareDepth;
        public int ParenthesisDepth;
        public bool InString;
        public bool InRawString;
        public bool Escaped;

        public readonly bool IsTopLevel
            => CurlyDepth is 0
                && SquareDepth is 0
                && ParenthesisDepth is 0
                && !InString
                && !InRawString;
    }
}
