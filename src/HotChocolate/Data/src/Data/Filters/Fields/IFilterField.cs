using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public interface IFilterField
        : IInputField
        , IHasRuntimeType
    {
        /// <summary>
        /// The type which declares this field.
        /// </summary>
        new IFilterInputType DeclaringType { get; }

        MemberInfo? Member { get; }

        new IExtendedType? RuntimeType { get; }

        IFilterFieldHandler Handler { get; }
    }
}
