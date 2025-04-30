using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanContext
{
    private readonly ISourceSchemaClientScope _clientScope;

    public OperationPlanContext(
        OperationPlan operationPlan,
        GraphQLRequestContext requestContext)
    {
        OperationPlan = operationPlan;
        RequestContext = requestContext;
        _clientScope = requestContext.RequestServices.GetRequiredService<ISourceSchemaClientScope>();
    }

    public OperationPlan OperationPlan { get; }

    public GraphQLRequestContext RequestContext { get; }

    public FetchResultStore ResultStore { get; } = new();

    public ImmutableArray<VariableValues> CreateVariables(
        ImmutableArray<int> dependencies,
        ImmutableHashSet<string> variables,
        ImmutableArray<OperationRequirement> requirements)
    {
        var results = ResultStore.GetResults(dependencies);

        foreach (var resultGroup in results.GroupBy(t => t.Target))
        {
            foreach (var requirement in requirements)
            {
                if (resultGroup.Key.IsParentOfOrSame(requirement.Path))
                {
                    foreach (var result in resultGroup)
                    {

                    }
                }
            }
        }
    }

    public ImmutableArray<VariableValues> CreateVariables2(
    ImmutableArray<int> dependencies,
    ImmutableHashSet<string> passThroughVariables,   // variables that are not covered by -requirements
    ImmutableArray<OperationRequirement> requirements)
    {
        // ───────────────────────────────────────────────────────────────
        // 1. Collect the results produced by all dependent execution
        //    nodes.  We look at them one-by-one; each “result” contains
        //    the JSON fragment that will eventually be patched into the
        //    overall response together with its runtime path metadata.
        // ───────────────────────────────────────────────────────────────
        var results = ResultStore.GetResults(dependencies);

        // Will hold  { runtimePath => { variableName => value } }
        var variableSets = new Dictionary<Path, Dictionary<string, object?>>(Path.Comparer);

        // Copy the variables that originate in the user request once so
        // we can seed every set with the same “passthrough” values.
        var requestVariables = RequestContext.Operation.Variables; // Hot Chocolate infra
        var passthroughTemplate = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var v in passThroughVariables)
        {
            if (requestVariables.TryGetValue(v, out var val))
            {
                passthroughTemplate[v] = val;
            }
        }

        // ───────────────────────────────────────────────────────────────
        // 2. Walk over each fetch-result and try to satisfy the declared
        //    requirements from the JSON it returned.
        // ───────────────────────────────────────────────────────────────
        foreach (var fetch in results)
        {
            foreach (var req in requirements)
            {
                // Only look at requirements that actually live *under*
                // the part of the tree this result will be patched into.
                if (!fetch.Target.IsParentOfOrSame(req.Path))
                {
                    continue;
                }

                // Compute the part of the path that is still “missing”
                // after we are at the target node, e.g.:
                //
                //   target      = /posts/author
                //   req.Path    = /posts/author/id
                //   relative    =            └─── id
                //
                var relative = req.Path.TrimStart(fetch.Target);

                // Inside the JSON payload we first have to step into the
                // `Source` selection (that is how the planner asked the
                // service), *then* follow the relative path.
                var selectionInPayload = fetch.Source.Append(relative);

                var element = fetch.GetFromData(selectionInPayload);
                if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                {
                    // Requirement not satisfiable from this result.
                    continue;
                }

                // Convert the JsonElement to the correct CLR value that
                // matches the variable’s declared GraphQL type.
                var clrValue = element.ToClrValue(req.Type);

                // The runtime path at which this variable-value belongs
                // is the result’s path plus the relative part.
                var runtimePath = fetch.Path.Append(relative);

                // ----------------------------------------------------------------
                // Merge the found value into the set that belongs to *that* path.
                // ----------------------------------------------------------------
                if (!variableSets.TryGetValue(runtimePath, out var set))
                {
                    set = new Dictionary<string, object?>(passthroughTemplate, StringComparer.Ordinal);
                    variableSets.Add(runtimePath, set);
                }

                set[req.Key] = clrValue;
            }
        }

        // ───────────────────────────────────────────────────────────────
        // 3. Materialise the dictionary into ImmutableArray<VariableValues>
        //    so the execution engine can hand the correct variables to
        //    the underlying source-schema client.
        // ───────────────────────────────────────────────────────────────
        var builder = ImmutableArray.CreateBuilder<VariableValues>(variableSets.Count);

        foreach ((var path, var dict) in variableSets)
        {
            builder.Add(new VariableValues(path, dict.ToImmutableDictionary()));
        }

        // Sort by runtime path so the caller gets them in a deterministic order.
        return builder.ToImmutable().Sort(static (a, b) => a.Path.CompareTo(b.Path));
    }

    // 1. Drop the leading segments of `prefix` from `full` and return what is left.
    public static SelectionPath TrimStart(this SelectionPath full, SelectionPath prefix)
    {
        var take = full.Segments.Count - prefix.Segments.Count;
        return take == 0
            ? SelectionPath.Root
            : new SelectionPath(full.Segments.Skip(prefix.Segments.Count).ToArray());
    }

    // 2. Append one path to another (`/a/b` + `c/d`  →  `/a/b/c/d`)
    public static SelectionPath Append(this SelectionPath basePath, SelectionPath addition)
        => addition == SelectionPath.Root
            ? basePath
            : new SelectionPath(basePath.Segments.Concat(addition.Segments).ToArray());

    // 3. Convert a JsonElement to the CLR representation that fits the
    //    variable’s GraphQL type (handles scalars and lists - enough for
    //    gateway variables; you can expand to input objects if you need).
    public static object? ToClrValue(this JsonElement element, ITypeNode type)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number =>
                type switch
                {
                    NamedTypeNode n when n.Name.Value is "Int" => element.GetInt32(),
                    _ => element.GetDouble()
                },
            JsonValueKind.True or JsonValueKind.False => element.GetBoolean(),
            JsonValueKind.Array => element.EnumerateArray()
                                         .Select(e => e.ToClrValue(((ListTypeNode)type).Type))
                                         .ToArray(),
            _ => null
        };
    }

    public ISourceSchemaClient GetClient(string schemaName)
        => _clientScope.GetClient(schemaName);
}

public static class SelectionPathExtensions
{
    /// <summary>base = /a/b , full = /a/b/c/d  ⇒  /c/d</summary>
    public static SelectionPath TrimStart(this SelectionPath full, SelectionPath @base)
    {
        if (!@base.IsParentOfOrSame(full))
            throw new ArgumentException("Base path is not a parent of the full path.");

        var skip = @base.Segments.Length;
        var segs = full.Segments.Skip(skip);
        var cur  = SelectionPath.Root;

        foreach (var s in segs)
            cur = s.Kind switch
            {
                SelectionPathSegmentKind.Field           => cur.AppendField(s.Name),
                SelectionPathSegmentKind.InlineFragment  => cur.AppendFragment(s.Name),
                _                                        => cur
            };

        return cur;
    }

    /// <summary>Concatenate:  /a/b   +  /c/d  ⇒  /a/b/c/d</summary>
    public static SelectionPath Append(this SelectionPath head, SelectionPath tail)
    {
        var cur = head;
        foreach (var s in tail.Segments.Skip(1))          // skip root
            cur = s.Kind switch
            {
                SelectionPathSegmentKind.Field           => cur.AppendField(s.Name),
                SelectionPathSegmentKind.InlineFragment  => cur.AppendFragment(s.Name),
                _                                        => cur
            };
        return cur;
    }
}