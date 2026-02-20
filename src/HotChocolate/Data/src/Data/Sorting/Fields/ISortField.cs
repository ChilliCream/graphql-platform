using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public interface ISortField
    : IInputValueDefinition
    , IRuntimeTypeProvider
{
    /// <summary>
    /// The type which declares this field.
    /// </summary>
    ISortInputType DeclaringType { get; }

    MemberInfo? Member { get; }

    new IExtendedType? RuntimeType { get; }

    ISortFieldHandler Handler { get; }

    ISortMetadata? Metadata { get; }
}
