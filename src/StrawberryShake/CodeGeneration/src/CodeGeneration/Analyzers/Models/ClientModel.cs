using HotChocolate;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Analyzers.Models;

/// <summary>
/// The client model represents the client with all its operations and types.
/// </summary>
public class ClientModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="ClientModel" />.
    /// </summary>
    /// <param name="schema">
    /// The GraphQL schema.
    /// </param>
    /// <param name="operations">
    /// The operations that the client has defined.
    /// </param>
    /// <param name="leafTypes">
    /// The leaf types that are used.
    /// </param>
    /// <param name="inputObjectTypes">
    /// The input types that could be passed in.
    /// </param>
    public ClientModel(
        ISchema schema,
        IReadOnlyList<OperationModel> operations,
        IReadOnlyList<LeafTypeModel> leafTypes,
        IReadOnlyList<InputObjectTypeModel> inputObjectTypes)
    {
        Operations = operations ??
                     throw new ArgumentNullException(nameof(operations));
        LeafTypes = leafTypes ??
                    throw new ArgumentNullException(nameof(leafTypes));
        InputObjectTypes = inputObjectTypes ??
                           throw new ArgumentNullException(nameof(inputObjectTypes));

        Schema = schema;

        var outputTypes = new Dictionary<string, OutputTypeModel>(StringComparer.Ordinal);
        var entities = new Dictionary<string, EntityModel>(StringComparer.Ordinal);

        foreach (var operation in operations)
        {
            foreach (var outputType in operation.OutputTypes)
            {
                if (!outputTypes.ContainsKey(outputType.Name))
                {
                    outputTypes.Add(outputType.Name, outputType);

                    if (!outputType.IsInterface &&
                        outputType.Type.IsEntity() &&
                        !entities.ContainsKey(outputType.Type.Name) &&
                        outputType.Type is IComplexOutputType complexOutputType)
                    {
                        entities.Add(outputType.Type.Name, new EntityModel(complexOutputType));
                    }
                }
            }
        }

        OutputTypes = outputTypes.Values.ToList();
        Entities = entities.Values.ToList();
    }

    /// <summary>
    /// The analyzed schema
    /// </summary>
    public ISchema Schema { get; }

    /// <summary>
    /// Gets the operations
    /// </summary>
    public IReadOnlyList<OperationModel> Operations { get; }

    /// <summary>
    /// Gets all the output types.
    /// </summary>
    public IReadOnlyList<OutputTypeModel> OutputTypes { get; }

    /// <summary>
    /// Gets the leaf types that are used by this operation.
    /// </summary>
    public IReadOnlyList<LeafTypeModel> LeafTypes { get; }

    /// <summary>
    /// Gets the input objects that are needed.
    /// </summary>
    public IReadOnlyList<InputObjectTypeModel> InputObjectTypes { get; }

    /// <summary>
    /// Gets the entities that are used in the operations.
    /// </summary>
    public IReadOnlyCollection<EntityModel> Entities { get; }
}
