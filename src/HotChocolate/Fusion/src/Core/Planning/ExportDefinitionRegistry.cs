using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal sealed class ExportDefinitionRegistry
{
    private readonly Dictionary<(ISelectionSet, string), string> _stateKeyLookup = new();
    private readonly Dictionary<string, ExportDefinition> _exportDefinitions = new(StringComparer.Ordinal);
    private readonly string _groupKey = "_fusion_exports_";
    private int _stateId;

    public IReadOnlyCollection<ExportDefinition> All => _exportDefinitions.Values;

    public string Register(
        ISelectionSet selectionSet,
        VariableDefinition variableDefinition,
        IExecutionStep executionStep)
    {
        var exportDefinition = new ExportDefinition(
            $"_{_groupKey}_{++_stateId}",
            selectionSet,
            variableDefinition,
            executionStep);
        _exportDefinitions.Add(exportDefinition.StateKey, exportDefinition);
        _stateKeyLookup.Add((selectionSet, variableDefinition.Name), exportDefinition.StateKey);
        return exportDefinition.StateKey;
    }

    public bool TryGetStateKey(
        ISelectionSet selectionSet,
        string variableName,
        [NotNullWhen(true)] out string? stateKey,
        [NotNullWhen(true)] out IExecutionStep? executionStep)
    {
        if (_stateKeyLookup.TryGetValue((selectionSet, variableName), out stateKey))
        {
            executionStep = _exportDefinitions[stateKey].ExecutionStep;
            return true;
        }

        stateKey = null;
        executionStep = null;
        return false;
    }

    public IReadOnlyList<VariableDefinitionNode> CreateVariableDefinitions(
        IReadOnlyCollection<string> stateKeys)
    {
        if (stateKeys.Count == 0)
        {
            return Array.Empty<VariableDefinitionNode>();
        }

        var definitions = new VariableDefinitionNode[stateKeys.Count];
        var index = 0;

        foreach (var stateKey in stateKeys)
        {
            var variableDefinition = _exportDefinitions[stateKey].VariableDefinition;
            definitions[index++] = new VariableDefinitionNode(
                null,
                new VariableNode(stateKey),
                variableDefinition.Type,
                null,
                Array.Empty<DirectiveNode>());
        }

        return definitions;
    }

    public IEnumerable<ISelectionNode> GetExportSelections(
        IExecutionStep executionStep,
        ISelectionSet selectionSet)
    {
        foreach (var exportDefinition in _exportDefinitions.Values)
        {
            if (ReferenceEquals(exportDefinition.ExecutionStep, executionStep) &&
                ReferenceEquals(exportDefinition.SelectionSet, selectionSet))
            {
                // TODO : we need to transform this for better selection during execution
                var selection = exportDefinition.VariableDefinition.Select;
                var stateKey = exportDefinition.StateKey;
                yield return selection.WithAlias(new NameNode(stateKey));
            }
        }
    }

}
