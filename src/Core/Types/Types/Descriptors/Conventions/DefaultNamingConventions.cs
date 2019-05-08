using System;
using System.Linq;
using System.Reflection;

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

            var description = parameter.GetGraphQLDescription();
            if (string.IsNullOrWhiteSpace(description))
            {
                description = parameter.GetXmlSummary();
            }

            return description;
        }

        public virtual NameString GetEnumValueName(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return value.ToString().ToUpperInvariant();
        }

        public virtual string GetEnumValueDescription(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Type enumType = value.GetType();
            MemberInfo enumValueMemberInfo = enumType
                .GetMember(value.ToString())
                .SingleOrDefault();
            if (enumValueMemberInfo == null)
            {
                return null;
            }

            return GetMemberDescription(enumValueMemberInfo, MemberKind.Field);
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

            var description = member.GetGraphQLDescription();
            if (string.IsNullOrWhiteSpace(description))
            {
                description = member.GetXmlSummary();
            }

            return description;
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

            var description = type.GetGraphQLDescription();
            if (string.IsNullOrWhiteSpace(description))
            {
                description = type.GetXmlSummary();
            }

            return description;
        }
    }
}
