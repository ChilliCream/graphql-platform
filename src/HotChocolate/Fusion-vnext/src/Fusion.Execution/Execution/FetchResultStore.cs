using System.Collections.Immutable;
#if NET9_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using System.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

// we must make this thread-safe
public sealed class FetchResultStore
{
    private readonly Dictionary<SelectionPath, List<FetchResult>> _resultsBySelectionPath = [];
    private readonly HashSet<SelectionPath> _selectionPaths = [];

    public void AddResult(FetchResult result)
    {
        _selectionPaths.Add(result.Target);

        if(!_resultsBySelectionPath.TryGetValue(result.Target, out var results))
        {
            results = [];
            _resultsBySelectionPath.Add(result.Target, results);
        }

        results.Add(result);
    }

    public IEnumerable<FetchResult> GetRootResults()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<FetchResult> GetResults(SelectionPath path)
    {
        foreach (var selectionPath in _selectionPaths)
        {
            if (!selectionPath.IsParentOfOrSame(path))
            {
                continue;
            }

            foreach (var result in _resultsBySelectionPath[selectionPath])
            {
                yield return result;
            }
        }
    }

    public IEnumerable<(Path Path, List<ObjectFieldNode> Fields)> GetValues(
        SelectionPath root,
        ImmutableArray<(string Key, FieldPath Map)> requirements)
    {
        ArgumentNullException.ThrowIfNull(root);

        var completed = new HashSet<Path>();

        foreach (var result in GetResults(root))
        {
            var relativeRoot = root.RelativeTo(result.Target);
            var rootElement = result.GetFromSourceData();

            if (rootElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            var current = new List<(JsonElement, Path)> { (rootElement, result.Path) };
            var next = new List<(JsonElement, Path)>();
            var currentList = new List<(JsonElement, Path)>();
            var nextList = new List<(JsonElement, Path)>();

            if (FanOutLists(result.Path, rootElement, currentList, nextList, next))
            {
                (current, next) = (next, current);
            }

            foreach (var segment in relativeRoot.Segments)
            {
                next.Clear();

                foreach (var (element, path) in current)
                {
                    if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                    {
                        continue;
                    }

                    if (segment.Kind == SelectionPathSegmentKind.InlineFragment)
                    {
                        // __typename discriminator
                        if (element.ValueKind == JsonValueKind.Object
                            && element.TryGetProperty("__typename", out var t)
                            && t.ValueKind == JsonValueKind.String
                            && t.ValueEquals(segment.Name))
                        {
                            next.Add((element, path));
                        }

                        continue;
                    }

                    if (element.ValueKind is not JsonValueKind.Object
                        || !element.TryGetProperty(segment.Name, out var property)
                        || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                    {
                        continue;
                    }

                    var nextPath = path.Append(segment.Name);

                    if (!FanOutLists(nextPath, property, currentList, nextList, next))
                    {
                        next.Add((property, nextPath));
                    }
                }

                (current, next) = (next, current);
            }

            foreach (var (element, path) in current)
            {
                if (!completed.Add(path)
                    || element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                {
                    continue;
                }

                var fields = new List<ObjectFieldNode>();

                foreach (var (key, map) in requirements)
                {
                    fields.Add(FieldPathExtractor.Extract(key, element, map));
                }

                yield return (path, fields);
            }
        }

        static bool FanOutLists(
            Path path,
            JsonElement element,
            List<(JsonElement, Path)> currentList,
            List<(JsonElement, Path)> nextList,
            List<(JsonElement, Path)> next)
        {
            if (element.ValueKind is not JsonValueKind.Array)
            {
                return false;
            }

            nextList.Clear();
            nextList.Add((element, path));
            var isList = false;

            do
            {
                (currentList, nextList) = (nextList, currentList);
                nextList.Clear();

                var idx = 0;
                foreach (var (listElement, listPath) in currentList)
                {
                    foreach (var item in listElement.EnumerateArray())
                    {
                        if(!isList && item.ValueKind == JsonValueKind.Array)
                        {
                            isList = true;
                        }

                        next.Add((item, listPath.Append(idx++)));
                    }
                }
            }
            while (isList);
            return true;
        }
    }

    private static class FieldPathExtractor
    {
        public static ObjectFieldNode Extract(string key, JsonElement element, FieldPath map)
        {
            var stack = map.Reverse().GetEnumerator();

            if (!stack.MoveNext())
            {
                throw new ArgumentException("The path must not be empty.", nameof(map));
            }

            var value = Visit(stack, element);
            return new ObjectFieldNode(key, value);
        }

        private static IValueNode Visit(IEnumerator<FieldPath> stack, JsonElement element)
        {
            var segment = stack.Current;

            if (!element.TryGetProperty(segment.Name, out var property) ||
                property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return NullValueNode.Default;
            }

            if (stack.MoveNext())
            {
                var next = Visit(stack, property);
                var field = new ObjectFieldNode(segment.Name, next);
                return new ObjectValueNode(field);
            }

            return Visit(property);
        }

        private static IValueNode Visit(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return new StringValueNode(element.GetString()!);

                case JsonValueKind.Number:
                    #if NET9_0_OR_GREATER
                    return Utf8GraphQLParser.Syntax.ParseValueLiteral(JsonMarshal.GetRawUtf8Value(element));
                    #else
                    return Utf8GraphQLParser.Syntax.ParseValueLiteral(element.GetRawText());
                    #endif

                case JsonValueKind.True:
                    return BooleanValueNode.True;

                case JsonValueKind.False:
                    return BooleanValueNode.False;

                case JsonValueKind.Array:
                    var items = new List<IValueNode>();
                    foreach (var item in element.EnumerateArray())
                    {
                        items.Add(Visit(item));
                    }
                    return new ListValueNode(items.ToImmutableArray());

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
