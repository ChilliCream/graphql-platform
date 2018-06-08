using System.Collections.Generic;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class InputObjectDefaultSerializerTests
    {
        [Fact]
        public void ParseValue()
        {
            // arrange
            Schema schema = Create();
            InputObjectType object1Type = schema.GetType<InputObjectType>("Object1");
            SerializationInputObject1 object1Instance = new SerializationInputObject1
            {
                Foo = new SerializationInputObject2()
            };

            // act
            IValueNode value = InputObjectDefaultSerializer.ParseValue(object1Type, object1Instance);

            // assert
            Assert.IsType<ObjectValueNode>(value);
            Assert.Equal(Snapshot.Current(), Snapshot.New(value));
        }

        [Fact]
        public void DetectLoop()
        {
            // arrange
            Schema schema = Create();
            InputObjectType object1Type = schema.GetType<InputObjectType>("Object1");
            SerializationInputObject1 object1Instance = new SerializationInputObject1
            {
                Foo = new SerializationInputObject2()
            };
            object1Instance.Foo.FooList.Add(object1Instance);

            // act
            IValueNode value = InputObjectDefaultSerializer.ParseValue(object1Type, object1Instance);

            // assert
            Assert.IsType<ObjectValueNode>(value);
            Assert.Equal(Snapshot.Current(), Snapshot.New(value));
        }

        public Schema Create()
        {
            return Schema.Create(c =>
            {
                c.RegisterType(new InputObjectType<SerializationInputObject1>(d =>
                {
                    d.Name("Object1");
                    d.Field(t => t.Foo).Type<InputObjectType<SerializationInputObject2>>();
                    d.Field(t => t.Bar).Type<StringType>();
                }));

                c.RegisterType(new InputObjectType<SerializationInputObject2>(d =>
                {
                    d.Name("Object2");
                    d.Field(t => t.FooList).Type<NonNullType<ListType<InputObjectType<SerializationInputObject1>>>>();
                }));
            });
        }
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
