using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Types.Relay.NodeConstants;

namespace HotChocolate.Types.Relay;

internal static class FieldsExtensions
{
    internal static T TryGetIdField<T>(this IEnumerable<T> fields)
        where T : IDefinition
    {
        return fields.FirstOrDefault(t => t.Name.EqualsOrdinal(Id));
    }
}
