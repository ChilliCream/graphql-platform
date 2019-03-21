using System;
using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class FieldBinding
    {
        public FieldBinding(
            NameString name,
            MemberInfo member,
            ObjectField field)
        {
            Name = name.EnsureNotEmpty(nameof(name));
            Member = member ?? throw new ArgumentNullException(nameof(member));
            Field = field ?? throw new ArgumentNullException(nameof(field));
        }

        public NameString Name { get; }

        public MemberInfo Member { get; }

        public ObjectField Field { get; }
    }
}
