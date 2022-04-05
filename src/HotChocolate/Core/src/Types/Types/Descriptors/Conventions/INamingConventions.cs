using System;
using System.Reflection;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public interface INamingConventions : IConvention
{
    NameString GetTypeName(Type type);

    NameString GetTypeName(Type type, TypeKind kind);

    string? GetTypeDescription(Type type, TypeKind kind);

    NameString GetMemberName(MemberInfo member, MemberKind kind);

    string? GetMemberDescription(MemberInfo member, MemberKind kind);

    NameString GetArgumentName(ParameterInfo parameter);

    string? GetArgumentDescription(ParameterInfo parameter);

    NameString GetEnumValueName(object value);

    string? GetEnumValueDescription(object value);

    bool IsDeprecated(MemberInfo member, out string? reason);

    bool IsDeprecated(object value, out string? reason);

    /// <summary>
    /// Formats a fieldName to abide by the current field naming convention.
    /// </summary>
    /// <param name="fieldName">
    /// The field name that needs formatting.
    /// </param>
    /// <returns>
    /// Returns a name string that has the correct naming format.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The field is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    NameString FormatFieldName(string fieldName);
}
