using System.Reflection;
using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Descriptors
{
    public interface ITypeInspector
    {
        IEnumerable<Type> GetResolverTypes(Type sourceType);
        IEnumerable<MemberInfo> GetMembers(Type type);
        ITypeReference GetReturnType(MemberInfo member, TypeContext context);
        IEnumerable<object> GetEnumValues(Type enumType);
    }
}
