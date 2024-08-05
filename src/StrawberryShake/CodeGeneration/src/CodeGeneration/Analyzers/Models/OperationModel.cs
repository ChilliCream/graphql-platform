using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers.Models;

public class OperationModel
{
    private readonly IReadOnlyDictionary<SelectionSetInfo, SelectionSetNode> _selectionSets;

    public OperationModel(
        string name,
        ObjectType type,
        DocumentNode document,
        OperationType operationType,
        IReadOnlyList<ArgumentModel> arguments,
        OutputTypeModel resultType,
        IReadOnlyList<LeafTypeModel> leafTypes,
        IReadOnlyList<InputObjectTypeModel> inputObjectTypes,
        IReadOnlyList<OutputTypeModel> outputTypeModels,
        IReadOnlyDictionary<SelectionSetInfo, SelectionSetNode> selectionSets)
    {
        Name = name.EnsureGraphQLName();
        Type = type ??
            throw new ArgumentNullException(nameof(type));
        Document = document ??
            throw new ArgumentNullException(nameof(document));
        OperationType = operationType;
        Arguments = arguments ??
            throw new ArgumentNullException(nameof(arguments));
        ResultType = resultType ??
            throw new ArgumentNullException(nameof(resultType));
        LeafTypes = leafTypes ??
            throw new ArgumentNullException(nameof(leafTypes));
        InputObjectTypes = inputObjectTypes ??
            throw new ArgumentNullException(nameof(inputObjectTypes));
        OutputTypes = outputTypeModels ??
            throw new ArgumentNullException(nameof(outputTypeModels));
        _selectionSets = selectionSets ??
            throw new ArgumentNullException(nameof(selectionSets));
    }

    public string Name { get; }

    public ObjectType Type { get; }

    public DocumentNode Document { get; }

    public OperationType OperationType { get; }

    public IReadOnlyList<ArgumentModel> Arguments { get; }

    public OutputTypeModel ResultType { get; }

    public IReadOnlyList<LeafTypeModel> LeafTypes { get; }

    public IReadOnlyList<InputObjectTypeModel> InputObjectTypes { get; }

    public IReadOnlyList<OutputTypeModel> OutputTypes { get; }

    public IEnumerable<OutputTypeModel> GetImplementations(OutputTypeModel outputType)
    {
        if (outputType is null)
        {
            throw new ArgumentNullException(nameof(outputType));
        }

        foreach (var model in OutputTypes)
        {
            if (model.Implements.Contains(outputType))
            {
                yield return model;
            }
        }
    }

    public OutputTypeModel GetFieldResultType(FieldNode fieldSyntax)
    {
        if (fieldSyntax is null)
        {
            throw new ArgumentNullException(nameof(fieldSyntax));
        }

        return OutputTypes.First(
            t => t.IsInterface && t.SelectionSet == fieldSyntax.SelectionSet);
    }

    public bool TryGetFieldResultType(
        FieldNode fieldSyntax,
        INamedType fieldNamedType,
        [NotNullWhen(true)] out OutputTypeModel? fieldType)
    {
        if (fieldSyntax is null)
        {
            throw new ArgumentNullException(nameof(fieldSyntax));
        }

        if(!_selectionSets.TryGetValue(
           new SelectionSetInfo(fieldNamedType, fieldSyntax.SelectionSet!),
           out var selectionSetNode))
        {
            selectionSetNode = fieldSyntax.SelectionSet;
        }

        fieldType = OutputTypes.FirstOrDefault(
            t => t.IsInterface && t.SelectionSet == selectionSetNode);

        return fieldType is not null;
    }
}
