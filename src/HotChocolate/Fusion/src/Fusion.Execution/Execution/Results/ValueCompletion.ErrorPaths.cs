using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed partial class ValueCompletion
{
    private bool CompleteOmittedDescendants(
        SourceResultElementSnapshot source,
        CompositeResultElement target,
        ErrorTrie errors,
        ResultSelectionSet? selections)
    {
        var context = target.GetObjectContext();

        foreach (var (segment, fieldErrors) in errors)
        {
            if (segment is not string sourceName
                || (source.ValueKind is JsonValueKind.Object
                    && (fieldErrors.Count == 0 || source.TryGetProperty(sourceName, out _))))
            {
                continue;
            }

            var runtimeType = context.SelectionSet?.Type;
            var name = selections?.GetResponseName(sourceName, runtimeType) ?? sourceName;
            if (context.TryGetVisibleProperty(name, out var field, out var selection)
                && _store.IsSelectionActive(selection)
                && !TryCompleteNullValue(
                    JsonValueKind.Undefined,
                    field,
                    fieldErrors,
                    selection,
                    runtimeType is null ? selections?.TryGetChild(name) : selections?.TryGetChild(name, runtimeType)))
            {
                return false;
            }
        }

        return true;
    }

    private void ReportCollapsedErrors(
        CompositeResultElement target,
        ErrorTrie errors,
        IType type,
        Selection selection,
        ResultSelectionSet? selections,
        bool isShared)
    {
        var context = GetErrorPathContext(target, type, selection, selections);
        ForwardError(errors.Error, context.Path);
        var path = ReportFirstDescendantError(errors, context);
        if (isShared && _propagateNullValues && context.Type is not null && path is not null)
        {
            (_sharedErrorPaths ??= []).Add(path);
        }
    }

    /// <summary>
    /// Propagates nulls for reported shared fields that are still missing from the merged result.
    /// </summary>
    private void CompleteSharedErrorPaths()
    {
        if (_sharedErrorPaths is not { Count: > 0 })
        {
            return;
        }

        foreach (var errorPath in _sharedErrorPaths)
        {
            var path = errorPath;
            CompositeResultElement field;
            while (!_store.TryGetResult(path, out field))
            {
                path = path.Parent;
            }

            if (!field.IsNullable && field.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                PropagateNullValues(field);
            }
        }

        _sharedErrorPaths.Clear();
    }

    private ErrorPathContext GetErrorPathContext(
        CompositeResultElement target,
        IType type,
        Selection selection,
        ResultSelectionSet? selections)
    {
        var path = Path.Root;
        foreach (var segment in target.CompactPath.Segments)
        {
            if (segment < 0)
            {
                path = path.Append(~segment);
                continue;
            }

            var field = target.Operation.GetSelectionById(segment);
            if (field.IsInternal || !_store.IsSelectionActive(field))
            {
                return new ErrorPathContext(path, null, selection, null);
            }

            path = path.Append(field.ResponseName);
        }

        return new ErrorPathContext(path, type, selection, selections);
    }

    /// <summary>
    /// Reports the first source error below a null value at the deepest matching client path.
    /// </summary>
    private Path? ReportFirstDescendantError(ErrorTrie errors, ErrorPathContext context)
    {
        var suffix = new List<object>();
        var error = GetFirstDescendantError(errors, suffix);
        if (error is null)
        {
            return null;
        }

        var path = context.Path;
        List<ErrorPathContext> candidates = [context];
        List<ErrorPathContext> matches = [];

        foreach (var segment in suffix)
        {
            foreach (var candidate in candidates)
            {
                AddMatchingSegments(candidate, segment, matches);
            }

            if (matches.Count == 0)
            {
                break;
            }

            path = matches[0].Path;
            (candidates, matches) = (matches, candidates);
            matches.Clear();
        }

        ForwardError(error, path);
        return path;
    }

    private void AddMatchingSegments(ErrorPathContext parent, object segment, List<ErrorPathContext> matches)
    {
        var type = parent.Type?.NullableType();
        if (type is ListType list && segment is int index)
        {
            matches.Add(parent with { Path = parent.Path.Append(index), Type = list.ElementType });
        }
        else if (type is ITypeDefinition namedType && namedType.IsCompositeType() && segment is string sourceName)
        {
            foreach (var possibleType in _schema.GetPossibleTypes(namedType))
            {
                var name = parent.Selections?.GetResponseName(sourceName, possibleType) ?? sourceName;
                if (parent.Selection.GetSelectionSet(possibleType) is { } selectionSet
                    && selectionSet.TryGetSelection(name, out var selection)
                    && !selection.IsInternal
                    && _store.IsSelectionActive(selection))
                {
                    var children = parent.Selections?.TryGetChild(name, possibleType);
                    var match = new ErrorPathContext(parent.Path.Append(name), selection.Type, selection, children);
                    if (!matches.Contains(match))
                    {
                        matches.Add(match);
                    }
                }
            }
        }
    }

    private static IError? GetFirstDescendantError(ErrorTrie trie, List<object> suffix)
    {
        Stack<Dictionary<object, ErrorTrie>.Enumerator>? parents = null;
        var children = trie.GetEnumerator();

        while (true)
        {
            if (!children.MoveNext())
            {
                if (parents is null || !parents.TryPop(out children))
                {
                    return null;
                }

                suffix.RemoveAt(suffix.Count - 1);
                continue;
            }

            var (segment, child) = children.Current;
            if (child.Error is { } error && !child.HasEarlierDescendantError)
            {
                suffix.Add(segment);
                return error;
            }

            if (child.Count > 0)
            {
                suffix.Add(segment);
                (parents ??= []).Push(children);
                children = child.GetEnumerator();
            }
        }
    }

    private readonly record struct ErrorPathContext(
        Path Path,
        IType? Type,
        Selection Selection,
        ResultSelectionSet? Selections);
}
