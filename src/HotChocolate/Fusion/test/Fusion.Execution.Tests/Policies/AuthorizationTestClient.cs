using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Policies;

/// <summary>
/// Answers every request with the data of an unrestricted source schema, reduced to the fields
/// the request selects and does not skip, and records the requests it received.
/// </summary>
internal sealed class AuthorizationTestClient(string data) : ISourceSchemaClient
{
    private readonly ConcurrentQueue<string> _requests = [];

    public ImmutableArray<string> Requests => [.. _requests];

    public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

    public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var sourceText = Encoding.UTF8.GetString(request.OperationSourceText.Value.Span);
        _requests.Enqueue(sourceText);

        var variables = ReadVariables(request);
        var operation = Utf8GraphQLParser.Parse(request.OperationSourceText.Value.Span)
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();

        var root = JsonNode.Parse(data)!.AsObject();
        Reduce(root, operation.SelectionSet, variables);

        var response = Encoding.UTF8.GetBytes(new JsonObject { ["data"] = root }.ToJsonString());
        var document = SourceResultDocument.Parse(
            context.MemorySource.GetNextArena(),
            response,
            response.Length);

        yield return new SourceSchemaResult(CompactPath.Root, document);
    }

    public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

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
