using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal sealed class TypeMergeHelper
{
    public static bool SameTypeShape(IType typeA, IType typeB)
    {
        while (true)
        {
            if (typeA is NonNullType && typeB is not NonNullType)
            {
                typeA = typeA.InnerType();

                continue;
            }

            if (typeB is NonNullType && typeA is not NonNullType)
            {
                typeB = typeB.InnerType();

                continue;
            }

            if (typeA is ListType || typeB is ListType)
            {
                if (typeA is not ListType || typeB is not ListType)
                {
                    return false;
                }

                typeA = typeA.InnerType();
                typeB = typeB.InnerType();

                continue;
            }

            return typeA.Equals(typeB, TypeComparison.Structural);
        }
    }

    /// <summary>
    /// Computes a single output type that is compatible with every type in <paramref name="fields"/>,
    /// considering all types together so that the result is independent of source schema order. The
    /// result is nullable unless every type is non-null, lists are merged element-wise, and differing
    /// composite named types are unified to the least restrictive declared supertype. Returns
    /// <c>false</c> when no such type exists (the fields are not mergeable).
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Least-Restrictive-Type">
    /// Specification
    /// </seealso>
    public static bool TryGetLeastRestrictiveType(
        IReadOnlyList<(IType Type, MutableSchemaDefinition Schema)> fields,
        [NotNullWhen(true)] out IType? result)
    {
        result = null;

        if (fields.Count == 0)
        {
            return false;
        }

        // The merged type is nullable unless every type is non-null.
        var isNullable = false;

        foreach (var (type, _) in fields)
        {
            if (type is not NonNullType)
            {
                isNullable = true;

                break;
            }
        }

        // Strip the non-null wrappers, keeping each type's owning schema.
        var unwrapped = new (IType Type, MutableSchemaDefinition Schema)[fields.Count];

        for (var i = 0; i < fields.Count; i++)
        {
            var (type, schema) = fields[i];
            unwrapped[i] = (type is NonNullType ? type.NullableType() : type, schema);
        }

        // If any type is a list, every type must be a list, and the element types are merged.
        var anyList = false;
        var allList = true;

        foreach (var (type, _) in unwrapped)
        {
            if (type is ListType)
            {
                anyList = true;
            }
            else
            {
                allList = false;
            }
        }

        if (anyList)
        {
            if (!allList)
            {
                return false;
            }

            var innerFields = new (IType, MutableSchemaDefinition)[unwrapped.Length];

            for (var i = 0; i < unwrapped.Length; i++)
            {
                innerFields[i] = (unwrapped[i].Type.InnerType(), unwrapped[i].Schema);
            }

            if (!TryGetLeastRestrictiveType(innerFields, out var innerType))
            {
                return false;
            }

            var listType = new ListType(innerType);
            result = isNullable ? listType : new NonNullType(listType);

            return true;
        }

        if (!TryGetLeastRestrictiveNamedOutputType(unwrapped, out var namedType))
        {
            return false;
        }

        result = isNullable ? namedType : new NonNullType(namedType);

        return true;
    }

    /// <summary>
    /// Selects one of the declared named types that is a supertype of every other declared named type.
    /// When several candidates qualify, the most specific one is chosen (smallest set of possible
    /// runtime object types, ties broken by type name) so that the selection is deterministic. Returns
    /// <c>false</c> when no declared type is a supertype of all the others.
    /// </summary>
    private static bool TryGetLeastRestrictiveNamedOutputType(
        IReadOnlyList<(IType Type, MutableSchemaDefinition Schema)> fields,
        [NotNullWhen(true)] out IType? result)
    {
        result = null;

        // The unique declared types, keyed by name, are the supertype candidates.
        var candidates = new Dictionary<string, ITypeDefinition>(StringComparer.Ordinal);

        foreach (var (type, _) in fields)
        {
            if (type is not ITypeDefinition namedType)
            {
                return false;
            }

            candidates[namedType.Name] = namedType;
        }

        // The same named type can be declared across several source schemas (for example, a union
        // whose members are split between schemas), so the possible runtime object types of each
        // candidate are aggregated across every participating schema. This keeps the selection
        // independent of source schema order.
        var possibleTypeNames = AggregatePossibleRuntimeObjectTypeNames(candidates.Keys, fields);

        List<ITypeDefinition>? supertypeCandidates = null;

        foreach (var candidate in candidates.Values)
        {
            var isSupertypeOfAll = true;

            // Every declared field type must be covered, including same-name definitions of a
            // different kind, which a supertype candidate must not unify.
            foreach (var (type, _) in fields)
            {
                if (!IsOutputSupertype(candidate, (ITypeDefinition)type, possibleTypeNames))
                {
                    isSupertypeOfAll = false;

                    break;
                }
            }

            if (isSupertypeOfAll)
            {
                (supertypeCandidates ??= []).Add(candidate);
            }
        }

        if (supertypeCandidates is null)
        {
            return false;
        }

        result = supertypeCandidates
            .OrderBy(c => possibleTypeNames[c.Name].Count)
            .ThenBy(c => c.Name, StringComparer.Ordinal)
            .First();

        return true;
    }

    /// <summary>
    /// Determines whether <paramref name="candidate"/> can represent every possible runtime object
    /// type of <paramref name="type"/>, making it a valid supertype for merging output field types.
    /// </summary>
    private static bool IsOutputSupertype(
        ITypeDefinition candidate,
        ITypeDefinition type,
        Dictionary<string, HashSet<string>> possibleTypeNames)
    {
        if (string.Equals(candidate.Name, type.Name, StringComparison.Ordinal))
        {
            // Same-name definitions are only mergeable when they share the same kind.
            return candidate.Kind == type.Kind;
        }

        if (candidate.IsLeafType() || type.IsLeafType())
        {
            return false;
        }

        // An object type is only a supertype of itself, which is handled by the name check above.
        if (candidate.Kind is TypeKind.Object)
        {
            return false;
        }

        // Every possible runtime object type of the type must also be a possible runtime object type
        // of the candidate.
        return possibleTypeNames[type.Name].IsSubsetOf(possibleTypeNames[candidate.Name]);
    }

    /// <summary>
    /// Builds the set of possible runtime object type names for each named type, aggregated across
    /// every source schema that declares it. Leaf types contribute no possible runtime object types.
    /// </summary>
    private static Dictionary<string, HashSet<string>> AggregatePossibleRuntimeObjectTypeNames(
        IEnumerable<string> typeNames,
        IReadOnlyList<(IType Type, MutableSchemaDefinition Schema)> fields)
    {
        var schemas = fields.Select(f => f.Schema).Distinct().ToArray();
        var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var typeName in typeNames)
        {
            var objectTypeNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var schema in schemas)
            {
                if (schema.Types.TryGetType(typeName, out var type)
                    && type.Kind is TypeKind.Object or TypeKind.Interface or TypeKind.Union)
                {
                    foreach (var objectType in schema.GetPossibleTypes(type))
                    {
                        objectTypeNames.Add(objectType.Name);
                    }
                }
            }

            result[typeName] = objectTypeNames;
        }

        return result;
    }
}
