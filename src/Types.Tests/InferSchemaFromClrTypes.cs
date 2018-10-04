using HotChocolate.Types;
using Xunit;

namespace HotChocolate
{
    public class InferSchemaFromClrTypes
    {
        [Fact]
        public void InferObjectTypes()
        {
            // arrange
            // act
            Schema schema = Schema.Create(c => c.RegisterQueryType<MyQuery>());

            // assert
            ObjectType type = schema.GetType<ObjectType>("MyQuery");
            Assert.NotNull(type);
            Assert.IsType<ObjectType<Foo>>(type.Fields["foo"].Type);
        }

        public class MyQuery
        {
            public Foo GetFoo() => new Foo();
        }

        public class Foo
        {
            public Bar GetBar() => new Bar();
        }

        public class Bar
        {
            public string Abc { get; set; } = "CDE";
        }
    }
}
