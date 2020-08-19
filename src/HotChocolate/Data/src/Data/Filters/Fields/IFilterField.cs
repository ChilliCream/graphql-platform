using System;
using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public interface IFilterField
        : IInputField
        , IHasRuntimeType
    {
        new IFilterInputType DeclaringType { get; }

        MemberInfo? Member { get; }

        IFilterFieldHandler? Handler { get; }

        bool? IsNullable { get; }

        FilterTypeInfo? TypeInfo { get; }

        Type? ElementType { get; }
    }
}
