using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.ApolloFederation;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Text.Json;
using ObjectFieldNode = HotChocolate.Language.ObjectFieldNode;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed partial class FetchResultStore
{
    /// <summary>
    /// Creates the representations variables of an entity lookup whose requirements read from
    /// contexts. The entities are grouped by equal context values, and every group gets its own
    /// variables with the representations of its entities and its context variable values.
    /// </summary>
    /// <returns>
    /// One representation value per group, in the order the groups first occur.
    /// </returns>
    public ImmutableArray<RepresentationValue> CreateRepresentationVariableValues(
        ReadOnlySpan<SelectionPath> selectionSets,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData,
        ImmutableArray<RepresentationShapeNode> requiredShape,
        string entityTypeName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(requestVariables);

        if (requiredData.Length == 0 || selectionSets.IsEmpty)
        {
            return [];
        }

        lock (_lock)
        {
            var count = CollectCombinedElementsUnsafe(selectionSets, ref _collectTargetCombined);

            if (count == 0)
            {
                return [];
            }

            var elements = _collectTargetCombined.AsSpan(0, count);
            var resolvers = CreateContextRootResolvers(requiredData, selectionSets[0]);
            using var groups = new ContextValueGroups(requiredData);
            var groupOfElement = new int[count];

            for (var i = 0; i < count; i++)
            {
                groupOfElement[i] = groups.AddElement(_schema, elements[i], requiredData, resolvers);
            }

            var result = ImmutableArray.CreateBuilder<RepresentationValue>(groups.Count);

            for (var group = 0; group < groups.Count; group++)
            {
                var members = new List<CompositeResultElement>();

                for (var i = 0; i < count; i++)
                {
                    if (groupOfElement[i] == group)
                    {
                        members.Add(elements[i]);
                    }
                }

                var representation = BuildRepresentationValueUnsafe(
                    CollectionsMarshal.AsSpan(members),
                    requestVariables,
                    requiredShape,
                    entityTypeName,
                    groups.GetValues(group));

                if (!representation.IsEmpty)
                {
                    result.Add(representation);
                }
            }

            return result.ToImmutable();
        }
    }

    /// <summary>
    /// Creates the representations variables of an entity lookup whose requirements read from
    /// contexts from the requirement values that the enclosing scope imported. The entities are
    /// grouped by equal context values, and every group gets its own variables.
    /// </summary>
    /// <returns>
    /// One representation value per group, in the order the groups first occur.
    /// </returns>
    internal ImmutableArray<RepresentationValue> CreateRepresentationVariableValuesFromSnapshot(
        ImmutableArray<VariableValues> importedEntries,
        HashSet<string> importedKeys,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData,
        ImmutableArray<RepresentationShapeNode> requiredShape,
        string entityTypeName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(importedKeys);
        ArgumentNullException.ThrowIfNull(requestVariables);

        if (importedEntries.IsDefaultOrEmpty || requiredData.Length == 0)
        {
            return [];
        }

        foreach (var requirement in requiredData)
        {
            if (!importedKeys.Contains(requirement.Key))
            {
                throw new InvalidOperationException(
                    "A deferred incremental plan fetch references a requirement that was not imported.");
            }
        }

        lock (_lock)
        {
            var slices = new (long Start, long Length)[requiredData.Length];
            using var groups = new ContextValueGroups(requiredData);
            var groupOfEntry = new int[importedEntries.Length];

            for (var i = 0; i < importedEntries.Length; i++)
            {
                groupOfEntry[i] = -1;

                if (importedEntries[i].IsEmpty)
                {
                    continue;
                }

                var sequence = importedEntries[i].Values.AsSequence();

                if (TryCollectRequirementSlices(sequence, requiredData, slices))
                {
                    groupOfEntry[i] = groups.AddSnapshot(sequence, slices);
                }
            }

            var result = ImmutableArray.CreateBuilder<RepresentationValue>(groups.Count);

            for (var group = 0; group < groups.Count; group++)
            {
                var members = new List<VariableValues>();

                for (var i = 0; i < importedEntries.Length; i++)
                {
                    if (groupOfEntry[i] == group)
                    {
                        members.Add(importedEntries[i]);
                    }
                }

                var representation = BuildRepresentationValueFromSnapshotUnsafe(
                    ImmutableCollectionsMarshal.AsImmutableArray(members.ToArray()),
                    requestVariables,
                    requiredData,
                    requiredShape,
                    entityTypeName,
                    groups.GetValues(group));

                if (!representation.IsEmpty)
                {
                    result.Add(representation);
                }
            }

            return result.ToImmutable();
        }
    }

    private void WriteContextVariablePropertiesUnsafe(ReadOnlySpan<ContextVariableValue> contextValues)
    {
        foreach (var contextValue in contextValues)
        {
            _jsonWriter.WritePropertyName(contextValue.Name);
            contextValue.Value.WriteTo(_jsonWriter);
        }
    }

    /// <summary>
    /// The value of a context variable that is sent next to the representations.
    /// </summary>
    private readonly record struct ContextVariableValue(string Name, JsonSegment Value);

    /// <summary>
    /// Groups the entities of a lookup by the values of the requirements that read from contexts.
    /// </summary>
    private sealed class ContextValueGroups : IDisposable
    {
        private readonly ChunkedArrayWriter _writer = new();
        private readonly JsonWriter _jsonWriter;
        private readonly int[] _requirementIndexes;
        private readonly string[] _names;
        private readonly (int Start, int Length)[] _valueSlices;
        private readonly Dictionary<int, int> _groupByHash = [];
        private readonly List<Group> _groups = [];

        public ContextValueGroups(ReadOnlySpan<OperationRequirement> requiredData)
        {
            var indexes = new List<int>();

            for (var i = 0; i < requiredData.Length; i++)
            {
                if (!requiredData[i].ContextRoots.IsDefaultOrEmpty)
                {
                    indexes.Add(i);
                }
            }

            _requirementIndexes = [.. indexes];
            _names = new string[_requirementIndexes.Length];
            _valueSlices = new (int, int)[_requirementIndexes.Length];

            for (var i = 0; i < _requirementIndexes.Length; i++)
            {
                _names[i] = requiredData[_requirementIndexes[i]].Key;
            }

            _jsonWriter = new JsonWriter(_writer, new JsonWriterOptions { Indented = false });
        }

        public int Count => _groups.Count;

        /// <summary>
        /// Adds the context values of <paramref name="element"/> and returns the group they
        /// belong to, or <c>-1</c> if a context value cannot be resolved for the element.
        /// </summary>
        public int AddElement(
            FusionSchemaDefinition schema,
            CompositeResultElement element,
            ReadOnlySpan<OperationRequirement> requiredData,
            ContextRootResolver?[] resolvers)
        {
            var start = _writer.Position;

            for (var i = 0; i < _requirementIndexes.Length; i++)
            {
                var requirementIndex = _requirementIndexes[i];
                var requirement = requiredData[requirementIndex];
                var contextRoots = ContextRootResolver.Resolve(resolvers[requirementIndex], element);
                var valueStart = _writer.Position;

                _jsonWriter.Reset(_writer);

                // A context path starts at the element that sets the context, so the entity
                // only serves as the base of the requirement.
                if (!ResultDataMapper.TryMap(
                    element,
                    requirement.Map,
                    requirement.Type,
                    schema,
                    contextRoots,
                    requirement.InternalAlias,
                    _jsonWriter))
                {
                    _writer.ResetTo(start);
                    return -1;
                }

                _valueSlices[i] = (valueStart, _writer.Position - valueStart);
                WriteSeparator();
            }

            return Commit(start);
        }

        /// <summary>
        /// Adds the imported context values of a snapshot entry and returns the group they
        /// belong to, or <c>-1</c> if the entry holds no value for a context requirement.
        /// </summary>
        public int AddSnapshot(
            ReadOnlySequence<byte> values,
            ReadOnlySpan<(long Start, long Length)> slices)
        {
            var start = _writer.Position;

            for (var i = 0; i < _requirementIndexes.Length; i++)
            {
                var (sliceStart, sliceLength) = slices[_requirementIndexes[i]];

                if (sliceStart < 0)
                {
                    _writer.ResetTo(start);
                    return -1;
                }

                var valueStart = _writer.Position;

                foreach (var memory in values.Slice(sliceStart, sliceLength))
                {
                    memory.Span.CopyTo(_writer.GetSpan(memory.Length));
                    _writer.Advance(memory.Length);
                }

                _valueSlices[i] = (valueStart, _writer.Position - valueStart);
                WriteSeparator();
            }

            return Commit(start);
        }

        public ContextVariableValue[] GetValues(int group)
        {
            var slices = _groups[group].Slices;
            var values = new ContextVariableValue[slices.Length];

            for (var i = 0; i < values.Length; i++)
            {
                values[i] = new ContextVariableValue(
                    _names[i],
                    JsonSegment.Create(_writer.GetSequence(slices[i].Start, slices[i].Length)));
            }

            return values;
        }

        public void Dispose() => _writer.Dispose();

        // The separator keeps the concatenated values of two different value lists apart.
        private void WriteSeparator()
        {
            _writer.GetSpan(1)[0] = (byte)',';
            _writer.Advance(1);
        }

        private int Commit(int start)
        {
            var length = _writer.Position - start;
            var hash = _writer.GetHashCode(start, length);
            var next = -1;

            if (_groupByHash.TryGetValue(hash, out var candidate))
            {
                next = candidate;

                while (candidate >= 0)
                {
                    var group = _groups[candidate];

                    if (group.Length == length && _writer.SequenceEqual(group.Start, start, length))
                    {
                        _writer.ResetTo(start);
                        return candidate;
                    }

                    candidate = group.Next;
                }
            }

            var index = _groups.Count;
            _groups.Add(new Group(start, length, [.. _valueSlices], next));
            _groupByHash[hash] = index;
            return index;
        }

        private readonly record struct Group(int Start, int Length, (int Start, int Length)[] Slices, int Next);
    }
}
