using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Builds a response-name group's parent-type/field-definition members
/// across a type region, shared by the ExactCases backend and its
/// case-budget fallback.
/// </summary>
internal static class TraversalMembers
{
    /// <summary>
    /// Builds one <see cref="CollectedFieldGroupMember"/> per possible type
    /// in <paramref name="region"/> that defines <paramref name="fieldName"/>,
    /// omitting types that do not (for example a meta field such as
    /// <c>__typename</c> on a schema with no introspection field
    /// definitions).
    /// </summary>
    public static CollectedFieldGroupMember[] Build(CostSchemaSnapshot snapshot, PossibleTypeSet region, string fieldName)
    {
        var members = new CollectedFieldGroupMember[region.Count];
        var count = 0;

        foreach (var typeIndex in region)
        {
            var type = snapshot.GetObjectTypeDefinition(typeIndex);

            if (type.Fields.TryGetField(fieldName, out var field))
            {
                members[count++] = new CollectedFieldGroupMember(type, field);
            }
        }

        return count == members.Length ? members : members[..count];
    }
}
