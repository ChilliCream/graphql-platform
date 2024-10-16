using System.Buffers;
using System.Text.Json;

namespace HotChocolate.Execution.Processing;

internal static class PathHelper
{
    private const int _initialPathLength = 64;

    public static Path CreatePathFromContext(ObjectResult parent)
    {
        if (parent.Parent is null)
        {
            if (parent.PatchPath is null)
            {
                return Path.Root;
            }

            return parent.PatchPath;
        }

        return CreatePath(parent);
    }

    public static Path CreatePathFromContext(ISelection selection, ResultData parent, int index)
        => parent switch
        {
            ObjectResult => CreatePath(parent, selection.ResponseName),
            ListResult => CreatePath(parent, index),
            _ => throw new NotSupportedException($"{parent.GetType().FullName} is not a supported parent type."),
        };

    public static Path CombinePath(Path path, JsonElement errorSubPath, int skipSubElements)
    {
        for (var i = skipSubElements; i < errorSubPath.GetArrayLength(); i++)
        {
            path = errorSubPath[i] switch
            {
                { ValueKind: JsonValueKind.String, } nameElement => path.Append(nameElement.GetString()!),
                { ValueKind: JsonValueKind.Number, } indexElement => path.Append(indexElement.GetInt32()),
                _ => throw new InvalidOperationException("The error path contains an unsupported element."),
            };
        }

        return path;
    }

    private static Path CreatePath(ResultData parent, object segmentValue)
    {
        var segments = ArrayPool<object>.Shared.Rent(_initialPathLength);
        segments[0] = segmentValue;
        var current = parent;
        var length = Build(segments, ref current);
        var path = CreatePath(current.PatchPath, segments, length);
        ArrayPool<object>.Shared.Return(segments);
        return path;
    }

    private static Path CreatePath(ResultData parent)
    {
        var segments = ArrayPool<object>.Shared.Rent(_initialPathLength);
        var current = parent;
        var length = Build(segments, ref current, 0);
        var path = CreatePath(current.PatchPath, segments, length);
        ArrayPool<object>.Shared.Return(segments);
        return path;
    }

    private static Path CreatePath(Path? patchPath, object[] segments, int length)
    {
        var root = patchPath ?? Path.Root;
        var path = root.Append((string) segments[length - 1]);

        if (length > 1)
        {
            for (var i = length - 2; i >= 0; i--)
            {
                path = segments[i] switch
                {
                    string s => path.Append(s),
                    int n => path.Append(n),
                    _ => path,
                };
            }
        }

        return path;
    }

    private static int Build(object[] segments, ref ResultData parent, int start = 1)
    {
        var segment = start;
        var current = parent;

        while (current.Parent is not null)
        {
            if (segments.Length <= segment)
            {
                var temp = ArrayPool<object>.Shared.Rent(segments.Length * 2);
                segments.AsSpan().CopyTo(temp);
                ArrayPool<object>.Shared.Return(segments);
                segments = temp;
            }

            var i = current.ParentIndex;
            var p = current.Parent;

            switch (p)
            {
                case ObjectResult o:
                {
                    var field = o[i];

                    if (!field.IsInitialized)
                    {
                        throw new InvalidOperationException("Cannot build path from an uninitialized field.");
                    }

                    segments[segment++] = field.Name;
                    current = o;
                    break;
                }

                case ListResult l:
                    segments[segment++] = i;
                    current = l;
                    break;

                default:
                    throw new NotSupportedException($"{p.GetType().FullName} is not a supported parent type.");
            }
        }

        parent = current;
        return segment;
    }
}
