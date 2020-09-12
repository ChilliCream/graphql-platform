using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting
{
    public interface ISortField
        : IInputField
        , IHasRuntimeType
    {
        /// <summary>
        /// The type which declares this field.
        /// </summary>
        new SortInputType DeclaringType { get; }

        MemberInfo? Member { get; }

        new IExtendedType? RuntimeType { get; }

        ISortFieldHandler Handler { get; }
    }
}
