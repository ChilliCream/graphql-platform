using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Descriptors
{
    public struct ConfigureNamedConvention
    {
        public ConfigureNamedConvention(string name, Type type, CreateConvention createConvention)
        {
            Name = name;
            Type = type;
            CreateConvention = createConvention;
        }

        public string Name { get; }
        public Type Type { get; }
        public CreateConvention CreateConvention { get; }

        public void Deconstruct(
            out string name,
            out Type type,
            out CreateConvention createConvention)
        {
            name = Name;
            type = Type;
            createConvention = CreateConvention;
        }
    }
}
