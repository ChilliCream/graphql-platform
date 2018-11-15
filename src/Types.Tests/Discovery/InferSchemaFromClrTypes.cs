using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Discovery
{
    public class InferSchemaFromClrTypes
    {
        [Fact]
        public void RegisterType_ClrClass_BecomesGraphQLObjectType()
        {
            // arrange
            // act
            Schema schema = Schema.Create(c => c.RegisterQueryType<MyQuery>());

            // assert
            ObjectType type = schema.GetType<ObjectType>("MyQuery");
            Assert.NotNull(type);
            Assert.IsType<ObjectType<Foo>>(type.Fields["foo"].Type);
        }

        [Fact]
        public void RegisterType_ClrEnumType_BecomesGraphQLEnumType()
        {
            // arrange
            // act
            Schema schema = Schema.Create(c =>
            {
                c.RegisterType<Baz>();
                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Baz");
            Assert.NotNull(type);
            Assert.Collection(type.Values,
                t => Assert.Equal("FOO", t.Name),
                t => Assert.Equal("BAR", t.Name));
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

        public enum Baz
        {
            Foo,
            Bar
        }
    }
}
