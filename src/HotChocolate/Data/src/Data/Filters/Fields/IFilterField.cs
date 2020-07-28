using System;
using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public interface IFilterField
        : IInputField
        , IHasRuntimeType
    {
        MemberInfo? Member { get; }

        FilterFieldHandler? Handler { get; }

        bool IsNullable { get; }

        Type? ElementType { get; }
    }
}
