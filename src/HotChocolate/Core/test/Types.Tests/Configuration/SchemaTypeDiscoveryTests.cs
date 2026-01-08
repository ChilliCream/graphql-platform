using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
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
        Assert.NotNull(schema.Types.GetType<IOutputTypeDefinition>("Foo"));
        Assert.NotNull(schema.Types.GetType<IOutputTypeDefinition>("Bar"));
        Assert.NotNull(schema.Types.GetType<IInputTypeDefinition>("FooInput"));
        Assert.NotNull(schema.Types.GetType<IInputTypeDefinition>("BarInput"));
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
        var query = schema.Types.GetType<ObjectType>("QueryField");
        Assert.NotNull(query);
        Assert.Collection(
            query.Fields.Where(t => !t.IsIntrospectionField),
            t => Assert.Equal("foo", t.Name));
        Assert.NotNull(schema.Types.GetType<ObjectType>("Foo"));
        Assert.NotNull(schema.Types.GetType<ObjectType>("Bar"));
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
        var query = schema.Types.GetType<ObjectType>("QueryProperty");
        Assert.NotNull(query);
        Assert.Collection(
            query.Fields.Where(t => !t.IsIntrospectionField),
            t => Assert.Equal("foo", t.Name));
        Assert.NotNull(schema.Types.GetType<ObjectType>("Foo"));
        Assert.NotNull(schema.Types.GetType<ObjectType>("Bar"));
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
        var query = schema.Types.GetType<ObjectType>("QueryMethodVoid");
        Assert.NotNull(query);
        Assert.Collection(query.Fields.Where(t => !t.IsIntrospectionField),
            t => Assert.Equal("foo", t.Name));
        Assert.NotNull(schema.Types.GetType<ObjectType>("Foo"));
        Assert.NotNull(schema.Types.GetType<ObjectType>("Bar"));
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
        var fooBar = schema.Types.GetType<EnumType>("FooBar");
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
        var fooByte = schema.Types.GetType<ObjectType>("FooByte");
        Assert.NotNull(fooByte);

        var field = fooByte.Fields["bar"];
        Assert.Equal("ByteArray", field.Type.NamedType().Name);
    }

    public class QueryFieldArgument(Bar bar)
    {
        public Bar Bar { get; } = bar;

        public Foo GetFoo(Foo foo)
        {
            return foo;
        }
    }

    public class QueryField
    {
        public Foo? GetFoo()
        {
            return null;
        }
    }

    public class QueryProperty(Foo foo)
    {
        public Foo Foo { get; } = foo;
    }

    public class QueryMethodVoid
    {
        public Foo? GetFoo()
        {
            return null;
        }

        public void GetBar()
        {
        }
    }

    public class QueryWithCustomScalar
    {
        public FooByte? GetFoo(FooByte foo)
        {
            return null;
        }
    }

    public class Foo
    {
        public required Bar Bar { get; set; }
    }

    public class FooByte
    {
        public required byte[] Bar { get; set; }
    }

    public class Bar
    {
        public required string Baz { get; set; }
    }

    public enum FooBar
    {
        Foo,
        Bar
    }

    public class ByteArrayType : ScalarType
    {
        public ByteArrayType() : base("ByteArray", BindingBehavior.Implicit)
        {
        }

        public override Type RuntimeType => typeof(byte[]);

        public override ScalarSerializationType SerializationType => ScalarSerializationType.String;

        public override object CoerceInputLiteral(IValueNode literal)
        {
            throw new NotSupportedException();
        }

        public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        {
            throw new NotSupportedException();
        }

        public override void CoerceOutputValue(object runtimeValue, ResultElement resultValue)
        {
            throw new NotSupportedException();
        }

        public override IValueNode ValueToLiteral(object runtimeValue)
        {
            throw new NotSupportedException();
        }
    }
}
