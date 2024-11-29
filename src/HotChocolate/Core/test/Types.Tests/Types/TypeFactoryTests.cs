namespace HotChocolate.Types;

public class TypeFactoryTests : TypeTestBase
{
    [Fact]
    public void CreateObjectType()
    {
        // arrange
        var source = @"
                type Simple {
                    a: String
                    b: [String]
                }
                schema { query: Simple }";

        var resolvers = new
        {
            Simple = new { A = "hello", B = new[] { "hello", }, },
        };

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .AddRootResolver(resolvers)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void ObjectFieldDeprecationReason()
    {
        // arrange
        var source = @"
                type Simple {
                    a: String @deprecated(reason: ""reason123"")
                }
                schema { query: Simple }";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .Use(_ => _)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void CreateObjectTypeDescriptions()
    {
        // arrange
        var source = @"
                ""SimpleDesc""
                type Simple {
                    ""ADesc""
                    a(""ArgDesc""arg: String): String
                }
                schema { query: Simple }";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .Use(_ => _)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void CreateInterfaceType()
    {
        // arrange
        var source = "interface Simple { a: String b: [String] }";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .AddQueryType<DummyQuery>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Simple");

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
        var source = @"
                interface Simple {
                    a: String @deprecated(reason: ""reason123"")
                }";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .AddQueryType<DummyQuery>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Simple");

        Assert.True(type.Fields["a"].IsDeprecated);
        Assert.Equal("reason123", type.Fields["a"].DeprecationReason);

        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void InterfaceFieldDeprecationWithoutReason()
    {
        // arrange
        var source = @"
                interface Simple {
                    a: String @deprecated
                }";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .AddQueryType<DummyQuery>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Simple");

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
            .Resolve("a"));

        var objectTypeB = new ObjectType(d => d
            .Name("B")
            .Field("a")
            .Type<StringType>()
            .Resolve("b"));

        var source = "union X = A | B";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .AddQueryType<DummyQuery>()
            .AddType(objectTypeA)
            .AddType(objectTypeB)
            .Create();

        // assert
        var type = schema.GetType<UnionType>("X");

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
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .AddQueryType<DummyQuery>()
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Abc");

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
        var source = @"
                enum Abc {
                    A
                    B @deprecated(reason: ""reason123"")
                    C
                }";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .AddQueryType<DummyQuery>()
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Abc");

        var value = type.Values.FirstOrDefault(t => t.Name == "B");
        Assert.NotNull(value);
        Assert.True(value.IsDeprecated);
        Assert.Equal("reason123", value.DeprecationReason);
    }

    [Fact]
    public void CreateInputObjectType()
    {
        // arrange
        var source = @"
                input Simple {
                    a: String @bind(to: ""Name"")
                    b: [String] @bind(to: ""Friends"")
                }";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .AddQueryType<DummyQuery>()
            .BindRuntimeType<SimpleInputObject>("Simple")
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("Simple");

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
        var source = "directive @foo(a:String) on QUERY";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .AddQueryType<DummyQuery>()
            .Create();

        // assert
        var type = schema.GetDirectiveType("foo");

        Assert.Equal("foo", type.Name);
        Assert.False(type.IsRepeatable);

        Assert.Collection(
            type.Locations.AsEnumerable(),
            t => Assert.Equal(DirectiveLocation.Query, t));

        Assert.Collection(
            type.Arguments,
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
        var source = "directive @foo(a:String) repeatable on QUERY";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .AddQueryType<DummyQuery>()
            .Create();

        // assert
        var type = schema.GetDirectiveType("foo");

        Assert.Equal("foo", type.Name);
        Assert.True(type.IsRepeatable);

        Assert.Collection(type.Locations.AsEnumerable(),
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
