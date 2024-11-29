using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public class ObjectTypeDescriptorTests : DescriptorTestBase
{
    [Fact]
    public void InferNameFromType()
    {
        // arrange
        var descriptor = new ObjectTypeDescriptor<Foo>(Context);

        // act
        IObjectTypeDescriptor<Foo> desc = descriptor;

        // assert
        Assert.Equal("Foo", descriptor.CreateDefinition().Name);
    }

    [Fact]
    public void GetNameFromAttribute()
    {
        // arrange
        var descriptor = new ObjectTypeDescriptor<Foo2>(Context);

        // act
        IObjectTypeDescriptor<Foo2> desc = descriptor;

        // assert
        Assert.Equal("FooAttr", descriptor.CreateDefinition().Name);
    }

    [Fact]
    public void OverwriteDefaultName()
    {
        // arrange
        var descriptor = new ObjectTypeDescriptor<Foo>(Context);

        // act
        descriptor.Name("FooBar");

        // assert
        Assert.Equal("FooBar", descriptor.CreateDefinition().Name);
    }

    [Fact]
    public void OverwriteAttributeName()
    {
        // arrange
        var descriptor = new ObjectTypeDescriptor<Foo2>(Context);

        // act
        IObjectTypeDescriptor<Foo2> desc = descriptor;
        desc.Name("FooBar");

        // assert
        Assert.Equal("FooBar", descriptor.CreateDefinition().Name);
    }

    [Fact]
    public void InferFieldsFromType()
    {
        // arrange
        var descriptor = new ObjectTypeDescriptor<Foo>(Context);

        // act
        IObjectTypeDescriptor<Foo> desc = descriptor;

        // assert
        Assert.Collection(
            descriptor.CreateDefinition().Fields
                .Select(t => t.Name)
                .OrderBy(t => t),
            t => Assert.Equal("a", t),
            t => Assert.Equal("b", t),
            t => Assert.Equal("c", t));
    }

    [Fact]
    public void IgnoreOverriddenPropertyField()
    {
        // arrange
        var descriptor = new ObjectTypeDescriptor<Foo>(Context);

        // act
        descriptor.Field(t => t.B).Ignore();

        // assert
        Assert.Collection(
            descriptor.CreateDefinition().Fields
                .Select(t => t.Name)
                .OrderBy(t => t),
            t => Assert.Equal("a", t),
            t => Assert.Equal("c", t));
    }

    [Fact]
    public void UnignoreOverriddenPropertyField()
    {
        // arrange
        var descriptor = new ObjectTypeDescriptor<Foo>(Context);

        // act
        descriptor.Field(t => t.B).Ignore();
        descriptor.Field(t => t.B).Ignore(false);

        // assert
        Assert.Collection(
            descriptor.CreateDefinition().Fields
                .Select(t => t.Name)
                .OrderBy(t => t),
            t => Assert.Equal("a", t),
            t => Assert.Equal("b", t),
            t => Assert.Equal("c", t));
    }

    [Fact]
    public void IgnoreOverriddenMethodField()
    {
        // arrange
        var descriptor = new ObjectTypeDescriptor<Foo>(Context);

        // act
        IObjectTypeDescriptor<Foo> desc = descriptor;
        desc.Field(t => t.Equals(default)).Ignore();

        // assert
        Assert.Collection(
            descriptor.CreateDefinition().Fields
                .Select(t => t.Name)
                .OrderBy(t => t),
            t => Assert.Equal("a", t),
            t => Assert.Equal("b", t),
            t => Assert.Equal("c", t));
    }

    [Fact]
    public void UnignoreOverriddenMethodField()
    {
        // arrange
        var descriptor = new ObjectTypeDescriptor<Foo>(Context);

        // act
        IObjectTypeDescriptor<Foo> desc = descriptor;
        desc.Field(t => t.Equals(default)).Ignore();
        desc.Field(t => t.Equals(default)).Ignore(false);

        // assert
        Assert.Collection(
            descriptor.CreateDefinition().Fields
                .Select(t => t.Name)
                .OrderBy(t => t),
            t => Assert.Equal("a", t),
            t => Assert.Equal("b", t),
            t => Assert.Equal("c", t),
            t => Assert.Equal("equals", t));
    }

    [Fact]
    public void DeclareFieldsExplicitly()
    {
        // arrange
        var descriptor = new ObjectTypeDescriptor<Foo>(Context);

        // act
        IObjectTypeDescriptor<Foo> desc = descriptor;
        desc.Field(t => t.A);
        desc.BindFields(BindingBehavior.Explicit);

        // assert
        Assert.Collection(
            descriptor.CreateDefinition().Fields.Select(t => t.Name),
            t => Assert.Equal("a", t));
    }

    [Fact]
    public async Task UseMiddleware()
    {
        // arrange
        var schema = SchemaBuilder.New().AddQueryType<BarType>().Create();
        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync("{ a b c}");

        // assert
        result.ToJson().MatchSnapshot();
    }

    public class Foo : FooBase
    {
        public string A { get; set; }
        public override string B { get; set; }
        public string C { get; set; }

        public override bool Equals(object obj) => true;

        public override int GetHashCode() => 0;
    }

    [GraphQLName("FooAttr")]
    public class Foo2 : FooBase
    {
    }

    public class FooBase
    {
        public virtual string B { get; set; }
    }

    public class BarType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Bar");

            descriptor.Field("a").Use(next => context =>
            {
                context.Result = "a_123";
                return next(context);
            }).Type<StringType>();

            descriptor.Field("b").Use<TestFieldMiddleware1>()
                .Type<StringType>();
            descriptor.Field("c").Use<TestFieldMiddleware2>()
                .Type<StringType>();
        }
    }

    public class TestFieldMiddleware1
    {
        private readonly FieldDelegate _next;

        public TestFieldMiddleware1(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public ValueTask InvokeAsync(IMiddlewareContext context)
        {
            context.Result = context.Selection.Field.Name + "_456";
            return _next(context);
        }
    }

    public class TestFieldMiddleware2
    {
        private FieldDelegate _next;

        public TestFieldMiddleware2(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public ValueTask InvokeAsync(IMiddlewareContext context)
        {
            context.Result = context.Selection.Field.Name + "_789";
            return _next(context);
        }
    }
}
