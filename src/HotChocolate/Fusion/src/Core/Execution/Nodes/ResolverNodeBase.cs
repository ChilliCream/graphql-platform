using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Language;
using static HotChocolate.Fusion.Utilities.Utf8QueryPlanPropertyNames;
using ThrowHelper = HotChocolate.Fusion.Utilities.ThrowHelper;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// The resolver node is responsible for executing a GraphQL request on a subgraph.
/// This represents the base class for various resolver node implementations.
/// </summary>
internal abstract partial class ResolverNodeBase : QueryPlanNode
{
    private readonly string _subgraphName;
    private readonly string _document;
    private readonly SelectionSet _selectionSet;
    private readonly string[] _provides;
    private readonly string[] _requires;
    private readonly string[] _forwardedVariables;
    private readonly string[] _path;
    private readonly TransportFeatures _transportFeatures;
    private readonly bool _hasDependencies;

    /// <summary>
    /// Initializes a new instance of <see cref="ResolverNodeBase"/>.
    /// </summary>
    /// <param name="id">
    /// The unique id of this node.
    /// </param>
    /// <param name="config">
    /// Gets the resolver configuration.
    /// </param>
    protected ResolverNodeBase(int id, Config config)
        : base(id)
    {
        config.ThrowIfNotInitialized();
        _subgraphName = config.SubgraphName;
        _document = config.Document;
        _selectionSet = config.SelectionSet;
        _provides = config.Provides;
        _requires = config.Requires;
        _forwardedVariables = config.ForwardedVariables;
        _path = config.Path;
        _transportFeatures = config.TransportFeatures;
        _hasDependencies = _requires.Length > 0 || _forwardedVariables.Length > 0;
    }

    /// <summary>
    /// Gets the name of the subgraph that is targeted by this resolver.
    /// </summary>
    protected string SubgraphName => _subgraphName;

    /// <summary>
    /// Gets the selection set for which data is being resolved.
    /// </summary>
    protected internal SelectionSet SelectionSet => _selectionSet;

    /// <summary>
    /// Gets the state that is being required by this resolver to be executed.
    /// </summary>
    protected string[] Requires => _requires;

    /// <summary>
    /// Gets the path from which the data has to be extracted.
    /// </summary>
    protected string[] Path => _path;

    /// <summary>
    /// Creates a GraphQL request with the specified variable values.
    /// </summary>
    /// <param name="variables">
    /// The variables that where available on the original request.
    /// </param>
    /// <param name="requirementValues">
    /// The variables values that where extracted from the parent request.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    protected SubgraphGraphQLRequest CreateRequest(
        IVariableValueCollection variables,
        IReadOnlyDictionary<string, IValueNode> requirementValues)
    {
        ObjectValueNode? vars = null;

        if (_hasDependencies)
        {
            var fields = new List<ObjectFieldNode>();

            if (_forwardedVariables.Length > 0)
            {
                ref var forwardedVariable = ref MemoryMarshal.GetArrayDataReference(_forwardedVariables);
                ref var end = ref Unsafe.Add(ref forwardedVariable, _forwardedVariables.Length);

                while (Unsafe.IsAddressLessThan(ref forwardedVariable, ref end))
                {
                    if (variables.TryGetVariable<IValueNode>(forwardedVariable, out var value) &&
                        value is not null)
                    {
                        value = ReformatVariableRewriter.Rewrite(value);
                        fields.Add(new ObjectFieldNode(forwardedVariable, value));
                    }

                    forwardedVariable = ref Unsafe.Add(ref forwardedVariable, 1)!;
                }
            }

            if (_requires.Length > 0)
            {
                ref var requirement = ref MemoryMarshal.GetArrayDataReference(_requires);
                ref var end = ref Unsafe.Add(ref requirement, _requires.Length);

                while (Unsafe.IsAddressLessThan(ref requirement, ref end))
                {
                    if (requirementValues.TryGetValue(requirement, out var value))
                    {
                        fields.Add(new ObjectFieldNode(requirement, value));
                    }
                    else
                    {
                        throw ThrowHelper.Requirement_Is_Missing(requirement, nameof(requirementValues));
                    }

                    requirement = ref Unsafe.Add(ref requirement, 1)!;
                }
            }

            vars = new ObjectValueNode(fields);
        }

        return new SubgraphGraphQLRequest(_subgraphName, _document, vars, null, _transportFeatures);
    }

    /// <summary>
    /// Unwraps the result from the GraphQL response that is needed by this query plan node.
    /// </summary>
    /// <param name="response">
    /// The GraphQL response.
    /// </param>
    /// <returns>
    /// The unwrapped result.
    /// </returns>
    protected JsonElement UnwrapResult(GraphQLResponse response)
    {
        if (_path.Length == 0)
        {
            return response.Data;
        }

        if (response.Data.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
        {
            var current = response.Data;

            ref var segment = ref MemoryMarshal.GetArrayDataReference(_path);
            ref var end = ref Unsafe.Add(ref segment, _path.Length);

            while (Unsafe.IsAddressLessThan(ref segment, ref end))
            {
                if (current.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                {
                    return current;
                }

                current.TryGetProperty(segment, out var propertyValue);
                current = propertyValue;
                segment = ref Unsafe.Add(ref segment, 1)!;
            }

            return current;
        }

        return response.Data;
    }

    /// <summary>
    /// Formats the properties of this query plan node in order to create a JSON representation.
    /// </summary>
    /// <param name="writer">
    /// The writer that is used to write the JSON representation.
    /// </param>
    protected override void FormatProperties(Utf8JsonWriter writer)
    {
        writer.WriteString(SubgraphProp, _subgraphName);
        writer.WriteString(DocumentProp, _document);
        writer.WriteNumber(SelectionSetIdProp, SelectionSet.Id);

        if (_path.Length > 0)
        {
            writer.WritePropertyName(PathProp);
            writer.WriteStartArray();

            ref var segment = ref MemoryMarshal.GetArrayDataReference(_path);
            ref var end = ref Unsafe.Add(ref segment, _path.Length);

            while (Unsafe.IsAddressLessThan(ref segment, ref end))
            {
                writer.WriteStringValue(segment);
                segment = ref Unsafe.Add(ref segment, 1)!;
            }

            writer.WriteEndArray();
        }

        if (_requires.Length > 0)
        {
            writer.WritePropertyName(RequiresProp);
            writer.WriteStartArray();

            ref var requirement = ref MemoryMarshal.GetArrayDataReference(_requires);
            ref var end = ref Unsafe.Add(ref requirement, _requires.Length);

            while (Unsafe.IsAddressLessThan(ref requirement, ref end))
            {
                writer.WriteStartObject();
                writer.WriteString(VariableProp, requirement);
                writer.WriteEndObject();

                requirement = ref Unsafe.Add(ref requirement, 1)!;
            }

            writer.WriteEndArray();
        }

        if (_provides.Length > 0)
        {
            writer.WritePropertyName(ProvidesProp);
            writer.WriteStartArray();

            ref var export = ref MemoryMarshal.GetArrayDataReference(_provides);
            ref var end = ref Unsafe.Add(ref export, _provides.Length);

            while (Unsafe.IsAddressLessThan(ref export, ref end))
            {
                writer.WriteStartObject();
                writer.WriteString(VariableProp, export);
                writer.WriteEndObject();

                export = ref Unsafe.Add(ref export, 1)!;
            }

            writer.WriteEndArray();
        }

        if (_forwardedVariables.Length > 0)
        {
            writer.WritePropertyName(ForwardedVariablesProp);
            writer.WriteStartArray();

            ref var variable = ref MemoryMarshal.GetArrayDataReference(_forwardedVariables);
            ref var end = ref Unsafe.Add(ref variable, _forwardedVariables.Length);

            while (Unsafe.IsAddressLessThan(ref variable, ref end))
            {
                writer.WriteStartObject();
                writer.WriteString(VariableProp, variable);
                writer.WriteEndObject();

                variable = ref Unsafe.Add(ref variable, 1)!;
            }
            writer.WriteEndArray();
        }
    }

    protected static ValueTask ReturnResult(GraphQLResponse response)
    {
        response.Dispose();
        return default;
    }

    protected static ValueTask ReturnResults(GraphQLResponse[] responses)
    {
        ref var response = ref MemoryMarshal.GetArrayDataReference(responses);
        ref var end = ref Unsafe.Add(ref response, responses.Length);

        while (Unsafe.IsAddressLessThan(ref response, ref end))
        {
            response.Dispose();
            response = ref Unsafe.Add(ref response, 1)!;
        }

        return default;
    }
}
