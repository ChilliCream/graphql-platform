using System;

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
    }
}
