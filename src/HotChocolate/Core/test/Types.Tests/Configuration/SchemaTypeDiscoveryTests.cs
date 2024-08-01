using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Configuration;

public class SchemaTypeDiscoveryTests
{
    [Fact]
    public void DiscoverInputArgumentTypes()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryFieldArgument>()
            .Create();

        // assert
        Assert.NotNull(schema.GetType<INamedOutputType>("Foo"));
        Assert.NotNull(schema.GetType<INamedOutputType>("Bar"));
        Assert.NotNull(schema.GetType<INamedInputType>("FooInput"));
        Assert.NotNull(schema.GetType<INamedInputType>("BarInput"));
    }

    [Fact]
    public void DiscoverOutputGraphFromMethod()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryField>()
            .Create();

        // assert
        var query = schema.GetType<ObjectType>("QueryField");
        Assert.NotNull(query);
        Assert.Collection(
            query.Fields.Where(t => !t.IsIntrospectionField),
            t => Assert.Equal("foo", t.Name));
        Assert.NotNull(schema.GetType<ObjectType>("Foo"));
        Assert.NotNull(schema.GetType<ObjectType>("Bar"));
    }

    [Fact]
    public void DiscoverOutputGraphFromProperty()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryProperty>()
            .Create();

        // assert
        var query = schema.GetType<ObjectType>("QueryProperty");
        Assert.NotNull(query);
        Assert.Collection(
            query.Fields.Where(t => !t.IsIntrospectionField),
            t => Assert.Equal("foo", t.Name));
        Assert.NotNull(schema.GetType<ObjectType>("Foo"));
        Assert.NotNull(schema.GetType<ObjectType>("Bar"));
    }

    [Fact]
    public void DiscoverOutputGraphAndIgnoreVoidMethods()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryMethodVoid>()
            .Create();

        // assert
        var query = schema.GetType<ObjectType>("QueryMethodVoid");
        Assert.NotNull(query);
        Assert.Collection(query.Fields.Where(t => !t.IsIntrospectionField),
            t => Assert.Equal("foo", t.Name));
        Assert.NotNull(schema.GetType<ObjectType>("Foo"));
        Assert.NotNull(schema.GetType<ObjectType>("Bar"));
    }

    [Fact]
    public void InferEnumAsEnumType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddType<FooBar>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var fooBar = schema.GetType<EnumType>("FooBar");
        Assert.NotNull(fooBar);
        Assert.Collection(fooBar.Values,
            t => Assert.Equal("FOO", t.Name),
            t => Assert.Equal("BAR", t.Name));
    }

    [Fact]
    public void InferCustomScalarTypes()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryWithCustomScalar>()
            .AddType<ByteArrayType>()
            .Create();

        // assert
        var fooByte = schema.GetType<ObjectType>("FooByte");
        Assert.NotNull(fooByte);

        var field = fooByte.Fields["bar"];
        Assert.Equal("ByteArray", field.Type.NamedType().Name);
    }

    public class QueryFieldArgument
    {
        public Bar Bar { get; }

        public Foo GetFoo(Foo foo)
        {
            return foo;
        }
    }

    public class QueryField
    {
        public Foo GetFoo()
        {
            return null;
        }
    }

    public class QueryProperty
    {
        public Foo Foo { get; }
    }

    public class QueryMethodVoid
    {
        public Foo GetFoo()
        {
            return null;
        }

        public void GetBar()
        {
        }
    }

    public class QueryWithCustomScalar
    {
        public FooByte GetFoo(FooByte foo)
        {
            return null;
        }
    }

    public class Foo
    {
        public Bar Bar { get; set; }
    }

    public class FooByte
    {
        public byte[] Bar { get; set; }
    }

    public class Bar
    {
        public string Baz { get; set; }
    }

    public enum FooBar
    {
        Foo,
        Bar,
    }

    public class ByteArrayType : ScalarType
    {
        public ByteArrayType() : base("ByteArray", BindingBehavior.Implicit)
        {
        }

        public override Type RuntimeType => typeof(byte[]);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotSupportedException();
        }

        public override object ParseLiteral(IValueNode literal)
        {
            throw new NotSupportedException();
        }

        public override IValueNode ParseValue(object value)
        {
            throw new NotSupportedException();
        }

        public override IValueNode ParseResult(object resultValue)
        {
            throw new NotSupportedException();
        }

        public override object Serialize(object runtimeValue)
        {
            throw new NotSupportedException();
        }

        public override object Deserialize(object resultValue)
        {
            throw new NotSupportedException();
        }

        public override bool TryDeserialize(object resultValue, out object runtimeValue)
        {
            throw new NotSupportedException();
        }

        public override bool TrySerialize(object runtimeValue, out object resultValue)
        {
            throw new NotSupportedException();
        }
    }
}
