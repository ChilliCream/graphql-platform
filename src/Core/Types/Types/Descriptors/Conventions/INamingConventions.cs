using System.Reflection;
using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public interface INamingConventions
    {
        NameString GetTypeName(Type type);

        string GetTypeDescription(Type type);

        NameString GetMemberName(MemberInfo member);

        string GetMemberDescription(MemberInfo member);

        NameString GetEnumValueName(object value);
    }
}
