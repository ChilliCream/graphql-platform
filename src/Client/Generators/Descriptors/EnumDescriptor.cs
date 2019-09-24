using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.Descriptors
{
    public class EnumDescriptor
        : IEnumDescriptor
    {
        public EnumDescriptor(EnumType type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var values = new List<IEnumValueDescriptor>();

            Name = type.Name;
            Values = values;

            foreach (EnumValue value in type.Values)
            {
                IDirective directive = value.Directives.FirstOrDefault(t =>
                    t.Name.Equals(GeneratorDirectives.Name)
                    && t.GetArgument<string>(GeneratorDirectives.ValueArgument) != null);

                string name = directive is null
                    ? GetPropertyName(value.Name.ToLowerInvariant())
                    : directive.GetArgument<string>(GeneratorDirectives.ValueArgument);

                values.Add(new EnumValueDescriptor(name, value.Name));
            }
        }

        public string Name { get; }

        public IReadOnlyList<IEnumValueDescriptor> Values { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            yield break;
        }
    }
}
