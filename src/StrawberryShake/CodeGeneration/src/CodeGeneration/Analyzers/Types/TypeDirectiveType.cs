using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Types
{
    /*
    public class TypeDirectiveType
        : DirectiveType<TypeDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<TypeDirective> descriptor)
        {
            descriptor.Name("type");
            descriptor.Argument(t => t.Name).Type<NonNullType<StringType>>();
            descriptor.Location(DirectiveLocation.Field);
        }
    }
    */

    public class RuntimeTypeDirective
    {
        public string Name { get; }
    }

    public class SerializationTypeDirective
    {
        public string Name { get; }
    }

    public class EnumValueDirective
    {
        public string Value { get; }
    }

    public class RenameDirective
    {
        public string Name { get; }
    }
}


/*
    @rename(name: "")

    @runtimeType(name: "")

    @serializationType(name: "")

    @enumValue(value: 1)



*/
