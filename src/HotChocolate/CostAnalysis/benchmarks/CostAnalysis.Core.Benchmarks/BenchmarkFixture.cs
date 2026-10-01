using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

internal static class BenchmarkFixture
{
    private static readonly string s_directives = ReadResource("cost-directives.graphql");

    public static MutableSchemaDefinition ParseSchema(string resourceName)
        => SchemaParser.Parse(s_directives + "\n" + ReadResource(resourceName));

    public static (DocumentNode Document, OperationDefinitionNode Operation) ParseOperation(
        string resourceName)
        => ParseOperationSource(ReadResource(resourceName));

    public static (DocumentNode Document, OperationDefinitionNode Operation) ParseOperationSource(
        string source)
    {
        var document = Utf8GraphQLParser.Parse(source);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        return (document, operation);
    }

    public static BenchmarkVariableValues Variables(
        params (string Name, IValueNode Value)[] values)
        => new(values.ToDictionary(pair => pair.Name, pair => pair.Value));

    private static string ReadResource(string resourceName)
        => File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "__resources__", resourceName));
}

internal sealed class BenchmarkVariableValues(
    IReadOnlyDictionary<string, IValueNode> values) : ICostVariableValues
{
    public bool TryGetValue(string name, out IValueNode? value)
    {
        if (values.TryGetValue(name, out var found))
        {
            value = found;
            return true;
        }

        value = null;
        return false;
    }
}
