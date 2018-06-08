using System.Collections.Generic;

namespace HotChocolate.Types
{
    public class InputObjectDefaultSerializerTests
    {
        public void Serialize()
        {
            InputObjectType<SerializationInputObject1> type =
                new InputObjectType<SerializationInputObject1>(d =>
                {

                });

        }




    }

    public class SerializationInputObject1
    {
        public SerializationInputObject2 Foo { get; set; } = new SerializationInputObject2();
        public string Bar { get; set; } = "Bar";
    }

    public class SerializationInputObject2
    {
        public List<SerializationInputObject1> FooList { get; set; } = new List<SerializationInputObject1>
        {
            new SerializationInputObject1
            {
                Foo = null
            }
        };
    }
}
