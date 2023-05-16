using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

internal readonly struct ExportDefinition
{
    public ExportDefinition(
        string stateKey,
        ISelectionSet selectionSet,
        FieldVariableDefinition variableDefinition,
        ExecutionStep executionStep)
    {
        StateKey = stateKey;
        SelectionSet = selectionSet;
        VariableDefinition = variableDefinition;
        ExecutionStep = executionStep;
    }

    public string StateKey { get; }

    public ISelectionSet SelectionSet { get; }

    public FieldVariableDefinition VariableDefinition { get; }

    public ExecutionStep ExecutionStep { get; }
}
