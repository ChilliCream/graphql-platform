using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal sealed class ExportDefinitionRegistry
{
    private readonly HashSet<string> _temp = [];
    private readonly Dictionary<(ISelectionSet, string), string> _stateKeyLookup = new();
    private readonly Dictionary<string, ExportDefinition> _exportLookup = new(StringComparer.Ordinal);
    private readonly List<ExportDefinition> _exports = [];
    private readonly string _groupKey = "_fusion_exports_";
    private int _stateId;

    public IReadOnlyCollection<ExportDefinition> All => _exportLookup.Values;

    public string Register(
        ISelectionSet selectionSet,
        FieldVariableDefinition variableDefinition,
        ExecutionStep providingExecutionStep)
    {
        if (_stateKeyLookup.TryGetValue((selectionSet, variableDefinition.Name), out var stateKey) &&
            _exportLookup.TryGetValue(stateKey, out var registeredExportDefinition) &&
            ReferenceEquals(registeredExportDefinition.ExecutionStep, providingExecutionStep))
        {
            return stateKey;
        }

        var key = $"_{_groupKey}_{++_stateId}";
        var exportDefinition = new ExportDefinition(key, selectionSet, variableDefinition, providingExecutionStep);
        _exportLookup.Add(exportDefinition.StateKey, exportDefinition);
        _stateKeyLookup.TryAdd((selectionSet, variableDefinition.Name), exportDefinition.StateKey);
        _exports.Add(exportDefinition);
        return exportDefinition.StateKey;
    }

    public void RegisterAdditionExport(
        FieldVariableDefinition variableDefinition,
        ExecutionStep providingExecutionStep,
        string stateKey)
    {
        var originalExport = _exportLookup[stateKey];
        var exportDefinition = new ExportDefinition(
            stateKey,
            originalExport.SelectionSet,
            variableDefinition,
            providingExecutionStep);
        _exports.Add(exportDefinition);
    }

    public bool TryGetStateKey(
        ISelectionSet selectionSet,
        string variableName,
        [NotNullWhen(true)] out string? stateKey,
        [NotNullWhen(true)] out ExecutionStep? executionStep)
    {
        if (_stateKeyLookup.TryGetValue((selectionSet, variableName), out stateKey))
        {
            executionStep = _exportLookup[stateKey].ExecutionStep;
            return true;
        }

        stateKey = null;
        executionStep = null;
        return false;
    }

    public IReadOnlyList<VariableDefinitionNode> CreateVariableDefinitions(
        IReadOnlySet<VariableDefinitionNode> forwardedVariables,
        IReadOnlyCollection<string> stateKeys,
        IReadOnlyDictionary<string, ITypeNode>? argumentTypes)
    {
        if (forwardedVariables.Count == 0 && (stateKeys.Count == 0 || argumentTypes is null))
        {
            return Array.Empty<VariableDefinitionNode>();
        }

        var definitions = new VariableDefinitionNode[stateKeys.Count + forwardedVariables.Count];
        var index = 0;

        if (stateKeys.Count != 0 && argumentTypes is not null)
        {
            foreach (var stateKey in stateKeys)
            {
                var variableDefinition = _exportLookup[stateKey].VariableDefinition;
                definitions[index++] = new VariableDefinitionNode(
                    null,
                    new VariableNode(stateKey),
                    argumentTypes[variableDefinition.Name],
                    null,
                    Array.Empty<DirectiveNode>());
            }
        }

        if (forwardedVariables.Count > 0)
        {
            foreach (var variableDefinitionNode in forwardedVariables)
            {
                definitions[index++] = variableDefinitionNode;
            }
        }

        return definitions;
    }

    public IEnumerable<ISelectionNode> GetExportSelections(
        ExecutionStep executionStep,
        ISelectionSet selectionSet)
    {
        _temp.Clear();

        foreach (var exportDefinition in _exports)
        {
            if (ReferenceEquals(exportDefinition.ExecutionStep, executionStep) &&
                ReferenceEquals(exportDefinition.SelectionSet, selectionSet) &&
                _temp.Add(exportDefinition.StateKey))
            {
                var selection = exportDefinition.VariableDefinition.Select;
                var stateKey = exportDefinition.StateKey;
                yield return selection.WithAlias(new NameNode(stateKey));
            }
        }
    }

    public IEnumerable<string> GetExportKeys(ExecutionStep executionStep)
    {
        _temp.Clear();

        foreach (var exportDefinition in _exports)
        {
            if (ReferenceEquals(exportDefinition.ExecutionStep, executionStep) &&
                _temp.Add(exportDefinition.StateKey))
            {
                yield return exportDefinition.StateKey;
            }
        }
    }

    public IEnumerable<string> GetExportKeys(SelectionExecutionStep executionStep)
    {
        _temp.Clear();

        if (executionStep.Variables.Count > 0)
        {
            foreach (var (_, key) in executionStep.Variables)
            {
                _temp.Add(key);
            }
        }

        foreach (var exportDefinition in _exports)
        {
            if (ReferenceEquals(exportDefinition.ExecutionStep, executionStep) &&
                _temp.Add(exportDefinition.StateKey))
            {
                yield return exportDefinition.StateKey;
            }
        }
    }
}
