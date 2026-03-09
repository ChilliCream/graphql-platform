using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public interface IFilterField : IInputValueDefinition, IInputValueInfo
{
    /// <summary>
    /// The type which declares this field.
    /// </summary>
    IFilterInputType DeclaringType { get; }

    MemberInfo? Member { get; }

    new IExtendedType? RuntimeType { get; }

    IFilterFieldHandler Handler { get; }

    IFilterMetadata? Metadata { get; }

    new IInputType Type { get; }
}
