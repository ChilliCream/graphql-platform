using System.Runtime.CompilerServices;
using HotChocolate.Caching.Memory;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Execution.Caching;

/// <summary>
/// Caches the variable coercion plan of a normalized operation, keyed by the operation node itself.
/// </summary>
internal sealed class VariableCoercionPlanCache(int capacity)
{
    private readonly Cache<OperationKey, VariableCoercionPlan> _cache = new(capacity);

    /// <summary>
    /// Gets the maximum number of plans that can be cached.
    /// </summary>
    public int Capacity => _cache.Capacity;

    /// <summary>
    /// Gets the number of plans currently cached.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Gets the plan of the <paramref name="operation"/> node,
    /// or creates it from the variable definitions of that node.
    /// </summary>
    public VariableCoercionPlan GetOrCreate(OperationDefinitionNode operation, ISchemaDefinition schema)
        => _cache.GetOrCreate(
            new OperationKey(operation),
            static (key, schema) => new VariableCoercionPlan(schema, key.Operation.VariableDefinitions),
            schema);

    /// <summary>
    /// Identifies an operation node by reference.
    /// </summary>
    private readonly struct OperationKey(OperationDefinitionNode operation) : IEquatable<OperationKey>
    {
        public OperationDefinitionNode Operation { get; } = operation;

        public bool Equals(OperationKey other)
            => ReferenceEquals(Operation, other.Operation);

        public override bool Equals(object? obj)
            => obj is OperationKey other && Equals(other);

        public override int GetHashCode()
            => RuntimeHelpers.GetHashCode(Operation);
    }
}
