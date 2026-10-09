using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Builds the parsed lookup documents, variable segments and items the alias batching tests
/// operate on.
/// </summary>
internal static class AliasBatchTestData
{
    public static Utf8OperationDocument ParseLookup(string sourceText)
        => Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(sourceText));

    public static AliasBatchItem CreateItem(
        ChunkedArrayWriter memory,
        Utf8OperationDocument document,
        string values,
        int requestIndex = 0,
        string lookupTypeName = "Product")
        => new(
            requestIndex,
            GetRootField(document),
            lookupTypeName,
            new VariableValues(default, CreateSegment(memory, values)));

    public static JsonSegment CreateSegment(ChunkedArrayWriter memory, string values)
    {
        var start = memory.Position;
        var bytes = Encoding.UTF8.GetBytes(values);
        bytes.CopyTo(memory.GetSpan(bytes.Length));
        memory.Advance(bytes.Length);
        return JsonSegment.Create(memory, start, memory.Position - start);
    }

    public static Utf8FieldNode GetRootField(Utf8OperationDocument document)
    {
        foreach (var operation in document.GetOperations())
        {
            foreach (var selection in operation.SelectionSet.GetSelections())
            {
                return selection.GetField();
            }
        }

        throw new InvalidOperationException("The document declares no root selection.");
    }

    public static Utf8VariableDefinitionNode GetVariableDefinition(
        Utf8OperationDocument document,
        string name)
    {
        var utf8Name = Encoding.UTF8.GetBytes(name);

        foreach (var operation in document.GetOperations())
        {
            foreach (var definition in operation.GetVariableDefinitions())
            {
                if (definition.Utf8Name.SequenceEqual(utf8Name))
                {
                    return definition;
                }
            }
        }

        throw new InvalidOperationException($"The document declares no variable '{name}'.");
    }
}
