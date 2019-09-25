using System.Linq;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class TypeFactoryTests
        : TypeTestBase
    {
        [Fact]
        public void CreateObjectType()
        {
            // arrange
            string source = "type Simple { a: String b: [String] }";

            // act
            var schema = Schema.Create(source, c =>
            {
                c.BindResolver(ctx =>
                    Task.FromResult<object>("hello"))
                    .To("Simple", "a");

                c.BindResolver(ctx =>
                    Task.FromResult<object>(new[] { "hello" }))
                    .To("Simple", "b");

                c.Options.QueryTypeName = "Simple";
            });

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectFieldDeprecationReason()
        {
            // arrange
            string source = @"
                type Simple {
                    a: String @deprecated(reason: ""reason123"")
                }";

            // act
            var schema = Schema.Create(source, c =>
            {
                c.Use(next => context => Task.CompletedTask);
                c.Options.QueryTypeName = "Simple";
            });

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void CreateObjectTypeDescriptions()
        {
            // arrange
            string source = @"
                ""SimpleDesc""
                type Simple {
                    ""ADesc""
                    a(""ArgDesc""arg: String): String
                }";

            // act
            var schema = Schema.Create(source, c =>
            {
                c.BindResolver(ctx =>
                    Task.FromResult<object>("hello"))
                    .To("Simple", "a");

                c.Options.QueryTypeName = "Simple";
            });

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void CreateInterfaceType()
        {
            // arrange
            string source = "interface Simple { a: String b: [String] }";

            // act
            var schema = Schema.Create(source, c =>
            {
                c.RegisterQueryType<DummyQuery>();
                c.Options.StrictValidation = false;
            });

            // assert
            InterfaceType type = schema.GetType<InterfaceType>("Simple");

            Assert.Equal("Simple", type.Name);
            Assert.Equal(2, type.Fields.Count);

            Assert.True(type.Fields.ContainsField("a"));
            Assert.False(type.Fields["a"].Type.IsNonNullType());
            Assert.False(type.Fields["a"].Type.IsListType());
            Assert.True(type.Fields["a"].Type.IsScalarType());
            Assert.Equal("String", type.Fields["a"].Type.TypeName());

            Assert.True(type.Fields.ContainsField("b"));
            Assert.False(type.Fields["b"].Type.IsNonNullType());
            Assert.True(type.Fields["b"].Type.IsListType());
            Assert.False(type.Fields["b"].Type.IsScalarType());
            Assert.Equal("String", type.Fields["b"].Type.TypeName());

            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void InterfaceFieldDeprecationReason()
        {
            // arrange
            string source = @"
                    interface Simple {
                        a: String @deprecated(reason: ""reason123"")
                    }";

            // act
            var schema = Schema.Create(source, c =>
            {
                c.RegisterQueryType<DummyQuery>();
                c.Options.StrictValidation = false;
            });

            // assert
            InterfaceType type = schema.GetType<InterfaceType>("Simple");

            Assert.True(type.Fields["a"].IsDeprecated);
            Assert.Equal("reason123", type.Fields["a"].DeprecationReason);

            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void InterfaceFieldDeprecationWithoutReason()
        {
            // arrange
            string source = @"
                interface Simple {
                    a: String @deprecated
                }";

            // act
            var schema = Schema.Create(source, c =>
            {
                c.Options.StrictValidation = false;
                c.RegisterQueryType<DummyQuery>();
            });

            // assert
            InterfaceType type = schema.GetType<InterfaceType>("Simple");

            Assert.True(type.Fields["a"].IsDeprecated);
            Assert.Equal(
                WellKnownDirectives.DeprecationDefaultReason,
                type.Fields["a"].DeprecationReason);

            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void CreateUnion()
        {
            // arrange
            var objectTypeA = new ObjectType(d => d
                .Name("A")
                .Field("a")
                .Type<StringType>()
                .Resolver("a"));

            var objectTypeB = new ObjectType(d => d
                .Name("B")
                .Field("a")
                .Type<StringType>()
                .Resolver("b"));

            var source = "union X = A | B";

            // act
            var schema = Schema.Create(source, c =>
            {
                c.RegisterType(objectTypeA);
                c.RegisterType(objectTypeB);
                c.RegisterQueryType<DummyQuery>();
            });

            // assert
            UnionType type = schema.GetType<UnionType>("X");

            Assert.Equal("X", type.Name);
            Assert.Equal(2, type.Types.Count);
            Assert.Equal("A", type.Types.First().Key);
            Assert.Equal("B", type.Types.Last().Key);
        }

        [Fact]
        public void CreateEnum()
        {
            // arrange
            var source = "enum Abc { A B C }";

            // act
            var schema = Schema.Create(source, c =>
            {
                c.RegisterQueryType<DummyQuery>();
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Abc");

            Assert.Equal("Abc", type.Name);
            Assert.Collection(type.Values,
                t => Assert.Equal("A", t.Name),
                t => Assert.Equal("B", t.Name),
                t => Assert.Equal("C", t.Name));
        }

        [Fact]
        public void EnumValueDeprecationReason()
        {
            // arrange
            string source = @"
                    enum Abc {
                        A
                        B @deprecated(reason: ""reason123"")
                        C
                    }";

            // act
            var schema = Schema.Create(source, c =>
            {
                c.RegisterQueryType<DummyQuery>();
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Abc");

            EnumValue value = type.Values.FirstOrDefault(t => t.Name == "B");
            Assert.NotNull(value);
            Assert.True(value.IsDeprecated);
            Assert.Equal("reason123", value.DeprecationReason);
        }

        [Fact]
        public void CreateInputObjectType()
        {
            // arrange
            string source = "input Simple { a: String b: [String] }";

            // act
            var schema = Schema.Create(
                source,
                c =>
                {
                    c.BindType<SimpleInputObject>()
                        .To("Simple")
                        .Field(t => t.Name).Name("a")
                        .Field(t => t.Friends).Name("b");
                    c.RegisterQueryType<DummyQuery>();
                });

            // assert
            InputObjectType type = schema.GetType<InputObjectType>("Simple");

            Assert.Equal("Simple", type.Name);
            Assert.Equal(2, type.Fields.Count);

            Assert.True(type.Fields.ContainsField("a"));
            Assert.False(type.Fields["a"].Type.IsNonNullType());
            Assert.False(type.Fields["a"].Type.IsListType());
            Assert.True(type.Fields["a"].Type.IsScalarType());
            Assert.Equal("String", type.Fields["a"].Type.TypeName());

            Assert.True(type.Fields.ContainsField("b"));
            Assert.False(type.Fields["b"].Type.IsNonNullType());
            Assert.True(type.Fields["b"].Type.IsListType());
            Assert.False(type.Fields["b"].Type.IsScalarType());
            Assert.Equal("String", type.Fields["b"].Type.TypeName());
        }

        [Fact]
        public void CreateDirectiveType()
        {
            // arrange
            string schemaSdl = "directive @foo(a:String) on QUERY";

            // act
            var schema = Schema.Create(
                schemaSdl,
                c => c.RegisterQueryType<DummyQuery>());

            // assert
            DirectiveType type = schema.GetDirectiveType("foo");
            Assert.Equal("foo", type.Name);
            Assert.False(type.IsRepeatable);
            Assert.Collection(type.Locations,
                t => Assert.Equal(DirectiveLocation.Query, t));
            Assert.Collection(type.Arguments,
                t =>
                {
                    Assert.Equal("a", t.Name);
                    Assert.IsType<StringType>(t.Type);
                });
        }

        [Fact]
        public void CreateRepeatableDirectiveType()
        {
            // arrange
            string schemaSdl = "directive @foo(a:String) repeatable on QUERY";

            // act
            var schema = Schema.Create(
                schemaSdl,
                c => c.RegisterQueryType<DummyQuery>());

            // assert
            DirectiveType type = schema.GetDirectiveType("foo");

            Assert.Equal("foo", type.Name);
            Assert.True(type.IsRepeatable);
            Assert.Collection(type.Locations,
                t => Assert.Equal(DirectiveLocation.Query, t));
            Assert.Collection(type.Arguments,
                t =>
                {
                    Assert.Equal("a", t.Name);
                    Assert.IsType<StringType>(t.Type);
                });
        }

        public class SimpleInputObject
        {
            public string Name { get; set; }
            public string[] Friends { get; set; }
        }

        public class DummyQuery
        {
            public string Bar { get; set; }
        }
    }
}
