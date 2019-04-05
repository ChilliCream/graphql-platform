using System.Reflection;
using System;

namespace HotChocolate.Types.Descriptors
{
    public class DefaultNamingConventions
        : INamingConventions
    {
        public virtual NameString GetArgumentName(ParameterInfo parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }
            return parameter.GetGraphQLName();
        }

        public virtual string GetArgumentDescription(ParameterInfo parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }
            return parameter.GetGraphQLDescription();
        }

        public virtual NameString GetEnumValueName(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return value.ToString().ToUpperInvariant();
        }

        public virtual NameString GetMemberName(
            MemberInfo member,
            MemberKind kind)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            return member.GetGraphQLName();
        }

        public virtual string GetMemberDescription(
            MemberInfo member,
            MemberKind kind)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            return member.GetGraphQLDescription();
        }

        public virtual NameString GetTypeName(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.GetGraphQLName();
        }

        public virtual NameString GetTypeName(Type type, TypeKind kind)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            string name = type.GetGraphQLName();

            if (kind == TypeKind.InputObject)
            {
                if (!name.EndsWith("Input", StringComparison.Ordinal))
                {
                    name = name + "Input";
                }
            }

            return name;
        }

        public string GetTypeDescription(Type type, TypeKind kind)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.GetGraphQLDescription();
        }
    }
}
