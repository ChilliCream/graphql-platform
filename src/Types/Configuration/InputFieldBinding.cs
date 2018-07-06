using System;
using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class InputFieldBinding
    {
        public InputFieldBinding(string name, PropertyInfo property, InputField field)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            Name = name;
            Property = property;
            Field = field;
        }

        public string Name { get; }
        public PropertyInfo Property { get; }
        public InputField Field { get; }
    }
}
