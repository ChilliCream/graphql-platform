using System;
using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class InputFieldBinding
    {
        public InputFieldBinding(
            NameString name,
            PropertyInfo property,
            InputField field)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            Name = name.EnsureNotEmpty(nameof(name));
            Property = property;
            Field = field;
        }

        public NameString Name { get; }

        public PropertyInfo Property { get; }

        public InputField Field { get; }
    }
}
