namespace HotChocolate.Fusion.Policies.Rego;

internal static class RegoEntrypointScanner
{
    public static List<string> Scan(string source)
    {
        var entryPoints = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var packageSeen = false;
        var metadataPending = false;
        var entrypointPending = false;

        foreach (var line in source.AsSpan().EnumerateLines())
        {
            var trimmed = line.Trim();

            if (trimmed.IsEmpty)
            {
                continue;
            }

            if (trimmed[0] is '#')
            {
                if (!packageSeen)
                {
                    continue;
                }

                if (IsMetadataStart(trimmed))
                {
                    metadataPending = true;
                    entrypointPending = false;
                }
                else if (metadataPending && IsEntrypointMetadata(trimmed))
                {
                    entrypointPending = true;
                }

                continue;
            }

            if (!packageSeen && IsPackageDeclaration(trimmed))
            {
                packageSeen = true;
                continue;
            }

            if (metadataPending)
            {
                if (entrypointPending && TryGetRuleName(trimmed, out var name) && seen.Add(name))
                {
                    entryPoints.Add(name);
                }

                metadataPending = false;
                entrypointPending = false;
            }
        }

        return entryPoints;
    }

    private static bool IsMetadataStart(ReadOnlySpan<char> line)
        => line.SequenceEqual("# METADATA");

    private static bool IsEntrypointMetadata(ReadOnlySpan<char> line)
    {
        var metadata = line[1..].TrimStart();
        return metadata.SequenceEqual("entrypoint: true");
    }

    private static bool IsPackageDeclaration(ReadOnlySpan<char> line)
        => StartsWithKeyword(line, "package");

    private static bool TryGetRuleName(ReadOnlySpan<char> line, out string name)
    {
        if (StartsWithKeyword(line, "default"))
        {
            line = line[7..].TrimStart();
        }

        var length = GetIdentifierLength(line);

        if (length == 0 || IsReservedKeyword(line[..length]))
        {
            name = string.Empty;
            return false;
        }

        name = line[..length].ToString();
        return true;
    }

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
}
