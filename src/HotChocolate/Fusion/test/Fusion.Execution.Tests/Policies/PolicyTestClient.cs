using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Policies;

/// <summary>
/// Answers every request, and every variable set of a request, with the data of an unrestricted
/// source schema, reduced to the fields the request selects and does not skip, and records the
/// requests it received. A subscription
/// delivers the events published to the client in the same reduced form.
/// </summary>
internal sealed class PolicyTestClient(string data) : ISourceSchemaClient
{
    private readonly ConcurrentQueue<string> _requests = [];
    private readonly Channel<string> _events = Channel.CreateUnbounded<string>();
    private int _subscribeCalls;

    public ImmutableArray<string> Requests => [.. _requests];

    public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

    public int SubscribeCalls => Volatile.Read(ref _subscribeCalls);

    public void Publish(string eventData)
        => _events.Writer.TryWrite(eventData);

    public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        if (request.Variables.IsEmpty)
        {
            yield return CreateResult(context, request, data, CompactPath.Root, default, recordRequest: true);
            yield break;
        }

        for (var i = 0; i < request.Variables.Length; i++)
        {
            var variables = request.Variables[i];

            yield return CreateResult(
                context,
                request,
                data,
                variables.Path,
                variables.AdditionalPaths,
                recordRequest: i == 0);
        }
    }

    public async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < requests.Length; i++)
        {
            await foreach (var result in ExecuteAsync(context, requests[i], cancellationToken))
            {
                yield return new SourceSchemaBatchResult(i, result);
            }
        }
    }

    public async IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _subscribeCalls);

        await foreach (var eventData in _events.Reader.ReadAllAsync(cancellationToken))
        {
            yield return CreateResult(context, request, eventData, CompactPath.Root, default, recordRequest: true);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private SourceSchemaResult CreateResult(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        string resultData,
        CompactPath path,
        CompactPathSegment additionalPaths,
        bool recordRequest)
    {
        if (recordRequest)
        {
            _requests.Enqueue(Encoding.UTF8.GetString(request.OperationSourceText.Value.Span));
        }

        var variables = ReadVariables(request);
        var operation = Utf8GraphQLParser.Parse(request.OperationSourceText.Value.Span)
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();

        var root = JsonNode.Parse(resultData)!.AsObject();
        Reduce(root, operation.SelectionSet, variables);

        var response = Encoding.UTF8.GetBytes(new JsonObject { ["data"] = root }.ToJsonString());
        var document = SourceResultDocument.Parse(
            context.MemorySource.GetNextArena(),
            response,
            response.Length);

        return new SourceSchemaResult(path, document, additionalPaths: additionalPaths);
    }

    private static Dictionary<string, bool> ReadVariables(SourceSchemaClientRequest request)
    {
        var variables = new Dictionary<string, bool>(StringComparer.Ordinal);

        foreach (var variableValues in request.Variables)
        {
            using var document = JsonDocument.Parse(variableValues.Values.AsSequence());

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    variables[property.Name] = property.Value.GetBoolean();
                }
            }
        }

        return variables;
    }

    private static void Reduce(
        JsonNode? node,
        SelectionSetNode selectionSet,
        Dictionary<string, bool> variables)
    {
        if (node is JsonArray array)
        {
            foreach (var element in array)
            {
                Reduce(element, selectionSet, variables);
            }

            return;
        }

        if (node is not JsonObject obj)
        {
            return;
        }

        var selected = new HashSet<string>(StringComparer.Ordinal);
        Collect(obj, selectionSet, variables, selected);

        foreach (var name in obj.Select(static p => p.Key).ToArray())
        {
            if (!selected.Contains(name))
            {
                obj.Remove(name);
            }
        }
    }

    private static void Collect(
        JsonObject obj,
        SelectionSetNode selectionSet,
        Dictionary<string, bool> variables,
        HashSet<string> selected)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field when !IsSkipped(field, variables):
                    var responseName = field.Alias?.Value ?? field.Name.Value;
                    selected.Add(responseName);

                    if (field.SelectionSet is not null && obj[responseName] is { } child)
                    {
                        Reduce(child, field.SelectionSet, variables);
                    }

                    break;

                case InlineFragmentNode fragment when !IsSkipped(fragment, variables):
                    Collect(obj, fragment.SelectionSet, variables, selected);
                    break;
            }
        }
    }

    private static bool IsSkipped(IHasDirectives node, Dictionary<string, bool> variables)
    {
        foreach (var directive in node.Directives)
        {
            if (directive.Name.Value == DirectiveNames.Skip.Name
                && directive.Arguments[0].Value is VariableNode variable
                && variables.GetValueOrDefault(variable.Name.Value))
            {
                return true;
            }
        }

        return false;
    }
}
