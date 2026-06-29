using System.Text;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed class ApolloEntityOperation
{
    public ApolloEntityOperation(
        OperationSourceText operation,
        string entityTypeName,
        string lookupFieldName,
        string inlineFragmentSourceText,
        ApolloRepresentationField[] representationFields)
    {
        ArgumentException.ThrowIfNullOrEmpty(entityTypeName);
        ArgumentException.ThrowIfNullOrEmpty(lookupFieldName);
        ArgumentNullException.ThrowIfNull(inlineFragmentSourceText);
        ArgumentNullException.ThrowIfNull(representationFields);

        Operation = operation;
        EntityTypeName = entityTypeName;
        LookupFieldName = lookupFieldName;
        InlineFragmentSourceText = inlineFragmentSourceText;
        RepresentationFields = representationFields;
    }

    public OperationSourceText Operation { get; }

    public string EntityTypeName { get; }

    public string LookupFieldName { get; }

    public string InlineFragmentSourceText { get; }

    public ApolloRepresentationField[] RepresentationFields { get; }
}

internal sealed class ApolloRepresentationField
{
    public ApolloRepresentationField(string variableName, string fieldName)
    {
        ArgumentException.ThrowIfNullOrEmpty(variableName);
        ArgumentNullException.ThrowIfNull(fieldName);

        VariableName = variableName;
        FieldName = fieldName;
        VariableNameUtf8 = Encoding.UTF8.GetBytes(variableName);
        FieldNameUtf8 = Encoding.UTF8.GetBytes(fieldName);
    }

    public string VariableName { get; }

    public string FieldName { get; }

    internal byte[] VariableNameUtf8 { get; }

    internal byte[] FieldNameUtf8 { get; }
}
