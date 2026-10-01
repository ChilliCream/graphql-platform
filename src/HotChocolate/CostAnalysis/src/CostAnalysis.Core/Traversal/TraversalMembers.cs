namespace HotChocolate.CostAnalysis;

/// <summary>
/// Provides the parent types and field definitions for a field selected within a type region.
/// </summary>
internal static class TraversalMembers
{
    /// <summary>
    /// Returns one member per possible type in <paramref name="region"/> that defines
    /// <paramref name="fieldName"/>. Types without that field are omitted.
    /// </summary>
    public static CollectedFieldGroupMember[] Build(
        CostSchemaIndex schemaIndex,
        PossibleTypeSet region,
        string fieldName)
    {
        var members = new CollectedFieldGroupMember[region.Count];
        var count = 0;

        foreach (var typeIndex in region)
        {
            var type = schemaIndex.GetObjectTypeDefinition(typeIndex);

            if (type.Fields.TryGetField(fieldName, out var field))
            {
                members[count++] = new CollectedFieldGroupMember(type, field);
            }
        }

        return count == members.Length ? members : members[..count];
    }
}
