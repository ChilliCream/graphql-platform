using System.Reflection;
using System;

namespace HotChocolate.Types.Descriptors
{
    public interface INamingConventions : IConvention
    {
        NameString GetTypeName(Type type);

        NameString GetTypeName(Type type, TypeKind kind);

        string GetTypeDescription(Type type, TypeKind kind);

        NameString GetMemberName(MemberInfo member, MemberKind kind);

        string GetMemberDescription(MemberInfo member, MemberKind kind);

        NameString GetArgumentName(ParameterInfo parameter);

        string GetArgumentDescription(ParameterInfo parameter);

        NameString GetEnumValueName(object value);

        string GetEnumValueDescription(object value);

        bool IsDeprecated(MemberInfo member, out string reason);

        bool IsDeprecated(object value, out string reason);
    }
}
