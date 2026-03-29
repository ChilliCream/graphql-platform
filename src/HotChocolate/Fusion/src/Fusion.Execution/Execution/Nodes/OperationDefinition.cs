using System.IO.Hashing;
using System.Text;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Nodes;

internal abstract class OperationDefinition : IOperationPlanNode
{
    private readonly OperationRequirement[] _requirements;
    private readonly string[] _forwardedVariables;
    private readonly ExecutionNodeCondition[] _conditions;
    private readonly ulong _operationHash;
    private IOperationPlanNode[] _dependents = [];
    private IOperationPlanNode[] _dependencies = [];
    private int _dependentCount;
    private int _dependencyCount;

    protected OperationDefinition(
        int id,
        OperationSourceText operation,
        string? schemaName,
        SelectionPath source,
        OperationRequirement[] requirements,
        string[] forwardedVariables,
        ResultSelectionSet resultSelectionSet,
        ExecutionNodeCondition[] conditions,
        bool requiresFileUpload)
    {
        Id = id;
        Operation = operation;
        _operationHash = XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(operation.SourceText));
        SchemaName = schemaName;
        Source = source;
        _requirements = requirements;
        _forwardedVariables = forwardedVariables;
        ResultSelectionSet = resultSelectionSet;
        _conditions = conditions;
        RequiresFileUpload = requiresFileUpload;
    }

    /// <summary>
    /// Gets the unique identifier of this operation definition within the plan.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the source text and metadata for the GraphQL operation that this
    /// definition represents.
    /// </summary>
    public OperationSourceText Operation { get; }

    /// <summary>
    /// Gets the xxhash64 of the operation source text. Precomputed during
    /// construction for use as a cache key by connectors.
    /// </summary>
    public ulong OperationHash => _operationHash;

    /// <summary>
    /// Gets the name of the source schema that this operation targets,
    /// or <c>null</c> when the schema is determined dynamically at runtime.
    /// </summary>
    public string? SchemaName { get; }

    /// <summary>
    /// Gets the path to the local selection set (the selection set within the
    /// source schema request) to extract the data from.
    /// </summary>
    public SelectionPath Source { get; }

    /// <summary>
    /// Gets the data requirements that must be satisfied before this operation
    /// can be executed.
    /// </summary>
    public ReadOnlySpan<OperationRequirement> Requirements => _requirements;

    /// <summary>
    /// Gets the names of the variables that are forwarded from the original
    /// client request into this operation.
    /// </summary>
    public ReadOnlySpan<string> ForwardedVariables => _forwardedVariables;

    /// <summary>
    /// Gets the result selection set that describes which fields this operation
    /// populates in the composite result.
    /// </summary>
    public ResultSelectionSet ResultSelectionSet { get; }

    /// <summary>
    /// Gets the conditions that control whether this operation is skipped.
    /// </summary>
    public ReadOnlySpan<ExecutionNodeCondition> Conditions => _conditions;

    /// <summary>
    /// Gets whether this operation contains one or more variables that reference
    /// the Upload scalar.
    /// </summary>
    public bool RequiresFileUpload { get; }

    /// <summary>
    /// Gets the nodes that cannot start until this operation definition has
    /// completed.
    /// </summary>
    public ReadOnlySpan<IOperationPlanNode> Dependents => _dependents.AsSpan(0, _dependentCount);

    /// <summary>
    /// Gets the nodes that must complete before this operation definition can
    /// start. These point to the original plan nodes (other operation definitions
    /// or standalone execution nodes), never to batch wrapper nodes.
    /// </summary>
    public ReadOnlySpan<IOperationPlanNode> Dependencies => _dependencies.AsSpan(0, _dependencyCount);

    /// <summary>
    /// Operation definitions never have optional dependencies, so this always
    /// returns an empty span.
    /// </summary>
    public ReadOnlySpan<IOperationPlanNode> OptionalDependencies => [];

    internal void AddDependency(IOperationPlanNode node)
    {
        if (_dependencies.Length == 0)
        {
            _dependencies = new IOperationPlanNode[4];
        }

        if (_dependencyCount == _dependencies.Length)
        {
            Array.Resize(ref _dependencies, _dependencyCount * 2);
        }

        _dependencies[_dependencyCount++] = node;
    }

    internal void AddDependent(IOperationPlanNode node)
    {
        if (_dependents.Length == 0)
        {
            _dependents = new IOperationPlanNode[4];
        }

        if (_dependentCount == _dependents.Length)
        {
            Array.Resize(ref _dependents, _dependentCount * 2);
        }

        _dependents[_dependentCount++] = node;
    }

    internal void Seal()
    {
        if (_dependencies.Length > _dependencyCount)
        {
            Array.Resize(ref _dependencies, _dependencyCount);
        }

        if (_dependents.Length > _dependentCount)
        {
            Array.Resize(ref _dependents, _dependentCount);
        }

        Array.Sort(_dependencies, static (a, b) => a.Id.CompareTo(b.Id));
        Array.Sort(_dependents, static (a, b) => a.Id.CompareTo(b.Id));
    }
}
