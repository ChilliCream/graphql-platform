using System.Collections.Generic;
using HotChocolate.Internal;
using Xunit;

namespace HotChocolate.Configuration
{
    public class TypeRegistryTests
    {
        [Fact]
        public void Foo()
        {
            // arrange
            TypeRegistry typeRegistry = new TypeRegistry(new ServiceManager());

            // act
            //typeRegistry.RegisterType()

            // assert

        }

        public class SerializationInputObject1
        {
            public SerializationInputObject2 Foo { get; set; }
            public string Bar { get; set; } = "Bar";
        }

        public class SerializationInputObject2
        {
            public List<SerializationInputObject1> FooList { get; set; } = new List<SerializationInputObject1>
        {
            new SerializationInputObject1()
        };
        }
    }
}
