using System.Collections.Immutable;
using HotChocolate.Execution.Properties;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal sealed class OperationCompilerOptimizers
{
    private ImmutableArray<IOperationOptimizer> _operationOptimizers;
    private ImmutableArray<ISelectionSetOptimizer> _selectionSetOptimizers;
    private PropertyInitFlags _initFlags;

    public ImmutableArray<IOperationOptimizer> OperationOptimizers
    {
        get => _operationOptimizers;
        set
        {
            if((_initFlags & PropertyInitFlags.OperationOptimizers) == PropertyInitFlags.OperationOptimizers)
            {
                throw new InvalidOperationException(
                    "OperationOptimizers can only be set once.");
            }

            _initFlags |= PropertyInitFlags.OperationOptimizers;
            _operationOptimizers = value;
        }
    }

    public ImmutableArray<ISelectionSetOptimizer> SelectionSetOptimizers
    {
        get => _selectionSetOptimizers;
        set
        {
            if((_initFlags & PropertyInitFlags.SelectionSetOptimizers) == PropertyInitFlags.SelectionSetOptimizers)
            {
                throw new InvalidOperationException(
                    "OperationOptimizers can only be set once.");
            }

            _initFlags |= PropertyInitFlags.SelectionSetOptimizers;
            _selectionSetOptimizers = value;
        }
    }

    [Flags]
    private enum PropertyInitFlags
    {
        OperationOptimizers = 1,
        SelectionSetOptimizers = 2
    }
}

public readonly struct OperationCompilerRequest
{
    public OperationCompilerRequest(
        string id,
        DocumentNode document,
        OperationDefinitionNode definition,
        ObjectType rootType,
        ISchema schema,
        ImmutableArray<IOperationOptimizer>? operationOptimizers = null,
        ImmutableArray<ISelectionSetOptimizer>? selectionSetOptimizers = null)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException(
                Resources.OperationCompiler_OperationIdNullOrEmpty,
                nameof(id));
        }

        Id = id;
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        RootType = rootType ?? throw new ArgumentNullException(nameof(rootType));
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        OperationOptimizers = operationOptimizers ?? ImmutableArray<IOperationOptimizer>.Empty;
        SelectionSetOptimizers = selectionSetOptimizers ?? ImmutableArray<ISelectionSetOptimizer>.Empty;
    }

    /// <summary>
    /// Gets the internal unique identifier for this operation.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the parsed query document that contains the
    /// operation-<see cref="Definition" />.
    /// </summary>
    public DocumentNode Document { get; }

    /// <summary>
    /// Gets the syntax node representing the operation definition.
    /// </summary>
    public OperationDefinitionNode Definition { get; }

    /// <summary>
    /// Gets the root type on which the operation is executed.
    /// </summary>
    public ObjectType RootType { get; }

    /// <summary>
    /// Gets the schema against which the operation shall be executed.
    /// </summary>
    public ISchema Schema { get; }

    public ImmutableArray<IOperationOptimizer> OperationOptimizers { get; }

    public ImmutableArray<ISelectionSetOptimizer> SelectionSetOptimizers { get; }
}
