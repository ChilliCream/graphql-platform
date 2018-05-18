using System;
using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class FieldBinding
    {
        public FieldBinding(string name, MemberInfo member, Field field)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            Name = name;
            Member = member;
            Field = field;
        }

        public string Name { get; }
        public MemberInfo Member { get; }
        public Field Field { get; }
    }
}
