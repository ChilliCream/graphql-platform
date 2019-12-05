using System.Reflection;
using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Descriptors
{
    public interface ITypeInspector
        : IConvention
    {
        IEnumerable<Type> GetResolverTypes(Type sourceType);

        IEnumerable<MemberInfo> GetMembers(Type type);

        /// <summary>
        /// Gets the field type from a <see cref="MemberInfo" />.
        /// </summary>
        /// <param name="member">
        /// The member from which the field type shall be extracted.
        /// </param>
        /// <param name="context">
        /// The context defines if the field has an input or output context.
        /// </param>
        /// <returns>
        /// Returns a type reference describing the type of the field.
        /// </returns>
        ITypeReference GetReturnType(MemberInfo member, TypeContext context);

        /// <summary>
        /// Gets the field argument type from a <see cref="ParameterInfo" />.
        /// </summary>
        /// <param name="parameter">
        /// The parameter from which the argument type shall be extracted.
        /// </param>
        /// <returns>
        /// Returns a type reference describing the type of the argument.
        /// </returns>
        ITypeReference GetArgumentType(ParameterInfo parameter);

        IEnumerable<object> GetEnumValues(Type enumType);

        MemberInfo GetEnumValueMember(object value);

        Type ExtractType(Type type);

        bool IsSchemaType(Type type);

        void ApplyAttributes(
            IDescriptor descriptor,
            ICustomAttributeProvider attributeProvider);
    }
}
