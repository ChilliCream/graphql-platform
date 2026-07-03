using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Pairs a <see cref="SelectionPath"/> with the type-system information needed to match its
/// inline-fragment segments against runtime data. Resolving the type conditions once when the
/// plan is built lets the result store match segments without consulting the schema during
/// execution.
/// </summary>
internal sealed class ResolvedSelectionPath
{
    private readonly FragmentSegment[]? _fragmentSegments;

    private ResolvedSelectionPath(SelectionPath path, FragmentSegment[]? fragmentSegments)
    {
        Path = path;
        _fragmentSegments = fragmentSegments;
    }

    /// <summary>
    /// Gets the underlying selection path.
    /// </summary>
    public SelectionPath Path { get; }

    /// <summary>
    /// Gets the resolved type condition of the inline-fragment segment at the specified index.
    /// </summary>
    public ITypeDefinition GetTypeCondition(int index)
        => _fragmentSegments![index].TypeCondition;

    /// <summary>
    /// Gets the object types that satisfy the inline-fragment type condition at the specified index.
    /// The result is empty when the condition is a concrete object type.
    /// </summary>
    public ImmutableArray<FusionObjectTypeDefinition> GetPossibleTypes(int index)
        => _fragmentSegments![index].PossibleTypes;

    /// <summary>
    /// Resolves the inline-fragment type conditions of <paramref name="path"/> against
    /// <paramref name="schema"/>.
    /// </summary>
    /// <param name="path">The selection path to resolve.</param>
    /// <param name="schema">The schema that provides the type conditions.</param>
    /// <returns>A <see cref="ResolvedSelectionPath"/> for <paramref name="path"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// An inline-fragment type condition does not exist in the schema.
    /// </exception>
    public static ResolvedSelectionPath Create(SelectionPath path, ISchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(schema);

        FragmentSegment[]? fragmentSegments = null;

        for (var i = 0; i < path.Length; i++)
        {
            var segment = path[i];

            if (segment.Kind is not SelectionPathSegmentKind.InlineFragment)
            {
                continue;
            }

            if (!schema.Types.TryGetType(segment.Name, out var type))
            {
                throw new InvalidOperationException(
                    $"The inline-fragment type condition '{segment.Name}' in the selection path "
                    + $"'{path}' does not exist in the schema.");
            }

            fragmentSegments ??= new FragmentSegment[path.Length];

            var possibleTypes = type.Kind is TypeKind.Interface or TypeKind.Union
                ? ((FusionSchemaDefinition)schema).GetPossibleTypes(type)
                : ImmutableArray<FusionObjectTypeDefinition>.Empty;

            fragmentSegments[i] = new FragmentSegment(type, possibleTypes);
        }

        return new ResolvedSelectionPath(path, fragmentSegments);
    }

    private readonly record struct FragmentSegment(
        ITypeDefinition TypeCondition,
        ImmutableArray<FusionObjectTypeDefinition> PossibleTypes);
}
