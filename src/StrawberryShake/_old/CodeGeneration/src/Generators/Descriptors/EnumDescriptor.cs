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
        public EnumDescriptor(string name, string ns, IReadOnlyList<IEnumValueDescriptor> values)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Namespace = ns ?? throw new ArgumentNullException(nameof(ns));
            Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public string Name { get; }

        public string Namespace { get; }

        public IReadOnlyList<IEnumValueDescriptor> Values { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            yield break;
        }
    }
}
