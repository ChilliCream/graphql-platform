using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Execution;

public class QueryExecutor
{
    public async ValueTask QueryAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var nodeMap = context.OperationPlan.AllNodes.ToDictionary(t => t.Id);
        var waitingToRun = new HashSet<ExecutionNode>(context.OperationPlan.AllNodes);
        var completed = new HashSet<ExecutionNode>();
        var running = new HashSet<Task<ExecutionStatus>>();

        foreach (var root in context.OperationPlan.RootNodes)
        {
            waitingToRun.Remove(root);
            running.Add(root.ExecuteAsync(context, cancellationToken));
        }

        while (running.Count > 0)
        {
            var task = await Task.WhenAny(running);
            running.Remove(task);

            var node = nodeMap[task.Result.Id];

            if (task.Result.IsSkipped)
            {
                // if a node is skipped, all dependents are skipped as well
                PurgeSkippedNodes(node, waitingToRun);
            }
            else
            {
                completed.Add(node);
                EnqueueNextNodes(context, waitingToRun, completed, running, cancellationToken);
            }
        }

        // assemble the result
    }

    private static void AssembleResult(
        OperationPlanContext context)
    {
        var path = Path.Root;
        var selectionPath = SelectionPath.Root;

        var errorPaths = new HashSet<Path>();
        var rootType = context.Schema.QueryType;

        using var arrayWriter = new PooledArrayWriter();
        using var jsonWriter = new Utf8JsonWriter(arrayWriter);

        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("data");

        if (errorPaths.Contains(path))
        {
            jsonWriter.WriteNullValue();
        }
        else
        {
            foreach (var rootResult in context.ResultStore.GetRootResults())
            {
                var rootElement = rootResult.GetFromSourceData();

                if (rootElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                {
                    jsonWriter.WriteNullValue();
                }
                else
                {
                    jsonWriter.WriteStartObject();

                    // JsonMarshal.GetRawUtf8Value()

                    jsonWriter.WriteEndObject();
                }
            }
        }

        jsonWriter.WriteEndObject();

        jsonWriter.Flush();
    }

    private void WriteFieldValue(
        OperationPlanContext context,
        Utf8JsonWriter writer,
        IComplexTypeDefinition type,
        HashSet<Path> errorPaths,
        Path path,
        SelectionPath selectionPath,
        FieldNode field,
        JsonElement parentData)
    {
        var fieldPath = path.Append(field.Name.Value);
        var responseName = field.Alias?.Value ?? field.Name.Value;

        if (errorPaths.Add(fieldPath))
        {
            writer.WriteNull(responseName);
            return;
        }

        var fieldDef = type.Fields[field.Name.Value];
        var fieldTypeKind = fieldDef.Type.AsTypeDefinition().Kind;

        if (!parentData.TryGetProperty(responseName, out var property)
            || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            writer.WriteNull(responseName);
            return;
        }

        if (fieldTypeKind is TypeKind.Scalar or TypeKind.Enum)
        {
            writer.WritePropertyName(responseName);
#if NET9_0_OR_GREATER
            writer.WriteRawValue(JsonMarshal.GetRawUtf8Value(property));
#else
            writer.WriteRawValue(property.GetRawText());
#endif
            return;
        }

        WriteCompositeValue(
            context,
            writer,
            type,
            errorPaths,
            fieldPath,
            selectionPath.AppendField(responseName),
            field.SelectionSet!,
            property);
    }

    private void WriteCompositeValue(
        OperationPlanContext context,
        Utf8JsonWriter writer,
        IType type,
        HashSet<Path> errorPaths,
        Path path,
        SelectionPath selectionPath,
        SelectionSetNode selectionSet,
        JsonElement parentData)
    {
        if (type.IsListType())
        {
            writer.WriteStartArray();

            var idx = 0;
            var itemType = type.InnerType();

            foreach (var item in parentData.EnumerateArray())
            {
                var itemPath = path.Append(idx++);

                if (errorPaths.Add(itemPath)
                    || item.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                {
                    writer.WriteNullValue();
                    continue;
                }

                WriteCompositeValue(
                    context,
                    writer,
                    itemType,
                    errorPaths,
                    itemPath,
                    selectionPath,
                    selectionSet,
                    item);
            }

            writer.WriteEndArray();
        }
        else
        {
        }
    }

    private static void EnqueueNextNodes(
        OperationPlanContext context,
        HashSet<ExecutionNode> waitingToRun,
        HashSet<ExecutionNode> completed,
        HashSet<Task<ExecutionStatus>> running,
        CancellationToken cancellationToken)
    {
        var selected = new List<ExecutionNode>();

        foreach (var node in waitingToRun)
        {
            if (completed.IsSupersetOf(node.Dependencies))
            {
                selected.Add(node);
            }
        }

        foreach (var node in selected)
        {
            waitingToRun.Remove(node);
            running.Add(node.ExecuteAsync(context, cancellationToken));
        }
    }

    private void PurgeSkippedNodes(ExecutionNode skipped, HashSet<ExecutionNode> waitingToRun)
    {
        var remove = new Stack<ExecutionNode>();
        remove.Push(skipped);

        while (remove.TryPop(out var node))
        {
            waitingToRun.Remove(node);

            foreach (var enqueuedNode in waitingToRun)
            {
                if (enqueuedNode.Dependencies.Contains(enqueuedNode))
                {
                    remove.Push(enqueuedNode);
                }
            }
        }
    }
}

public sealed class SelectionVisitor
{
    // public IEnumerable<FieldNode>
}
