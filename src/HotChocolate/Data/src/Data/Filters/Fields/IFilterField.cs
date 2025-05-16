using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public interface IFilterField : IInputValueDefinition
{
    /// <summary>
    /// The type which declares this field.
    /// </summary>
    IFilterInputType DeclaringType { get; }

    MemberInfo? Member { get; }

    IExtendedType? RuntimeType { get; }

    IFilterFieldHandler Handler { get; }

    IFilterMetadata? Metadata { get; }
}
