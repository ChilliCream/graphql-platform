using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Pairs a <see cref="SelectionPath"/> with the type conditions of its inline-fragment segments
/// resolved against the schema, so that segments can be matched against runtime data by type
/// identity.
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
    /// Gets the resolved type condition of the inline-fragment segment at the specified index,
    /// or <c>null</c> when the type condition could not be resolved against the schema and is
    /// matched by name instead.
    /// </summary>
    public ITypeDefinition? GetTypeCondition(int index)
        => _fragmentSegments![index].TypeCondition;

    /// <summary>
    /// Gets the object types that satisfy the inline-fragment type condition at the specified index.
    /// The result is empty when the condition is a concrete object type or could not be resolved.
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
    public static ResolvedSelectionPath Create(SelectionPath path, FusionSchemaDefinition schema)
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

            fragmentSegments ??= new FragmentSegment[path.Length];

            // A type condition the schema cannot resolve (for example a type removed from the
            // schema after a plan was cached) is kept as a name-only segment. Such segments match
            // by type-name equality, preserving the behavior of the original string-based matching.
            if (schema.Types.TryGetType(segment.Name, allowInaccessibleFields: true, out var type))
            {
                var possibleTypes = type.Kind is TypeKind.Interface or TypeKind.Union
                    ? schema.GetPossibleTypes(type)
                    : ImmutableArray<FusionObjectTypeDefinition>.Empty;

                fragmentSegments[i] = new FragmentSegment(type, possibleTypes);
            }
            else
            {
                fragmentSegments[i] = new FragmentSegment(
                    TypeCondition: null,
                    ImmutableArray<FusionObjectTypeDefinition>.Empty);
            }
        }

        return new ResolvedSelectionPath(path, fragmentSegments);
    }

    private readonly record struct FragmentSegment(
        ITypeDefinition? TypeCondition,
        ImmutableArray<FusionObjectTypeDefinition> PossibleTypes);
}
