using System.Reflection;
using System;

namespace HotChocolate.Types.Descriptors
{
    public class DefaultNamingConventions
        : INamingConventions
    {
        public NameString GetEnumValueName(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return value.ToString().ToUpperInvariant();
        }

        public string GetMemberDescription(MemberInfo member, MemberKind kind)
        {
            throw new NotImplementedException();
        }

        public NameString GetMemberName(MemberInfo member, MemberKind kind)
        {
            throw new NotImplementedException();
        }

        public string GetTypeDescription(Type type, TypeKind kind)
        {
            throw new NotImplementedException();
        }

        public NameString GetTypeName(Type type, TypeKind kind)
        {
            string name = null;

            if (kind == TypeKind.InputObject)
            {
                if (!name.EndsWith("Input", StringComparison.Ordinal))
                {
                    name = name + "Input";
                }
            }

            throw new NotImplementedException();
        }
    }
}
