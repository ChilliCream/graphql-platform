using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public class ObjectTypeDescriptorTests : DescriptorTestBase
{
    [Fact]
    public void InferNameFromType()
    {
        // arrange & act
        var descriptor = new ObjectTypeDescriptor<Foo>(Context);

        // assert
        Assert.Equal("Foo", descriptor.CreateConfiguration().Name);
    }

    [Fact]
    public void GetNameFromAttribute()
    {
        // arrange & act
        var descriptor = new ObjectTypeDescriptor<Foo2>(Context);

        // assert
        Assert.Equal("FooAttr", descriptor.CreateConfiguration().Name);
    }

    [Fact]
    public void OverwriteDefaultName()
    {
        // arrange
        var descriptor = new ObjectTypeDescriptor<Foo>(Context);

        // act
        descriptor.Name("FooBar");

        // assert
        Assert.Equal("FooBar", descriptor.CreateConfiguration().Name);
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
        Assert.Equal("FooBar", descriptor.CreateConfiguration().Name);
    }

    [Fact]
    public void InferFieldsFromType()
    {
        // arrange & act
        var descriptor = new ObjectTypeDescriptor<Foo>(Context);

        // assert
        Assert.Collection(
            descriptor.CreateConfiguration().Fields
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
            descriptor.CreateConfiguration().Fields
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
            descriptor.CreateConfiguration().Fields
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
        desc.Field(t => t.Equals(null)).Ignore();

        // assert
        Assert.Collection(
            descriptor.CreateConfiguration().Fields
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
        desc.Field(t => t.Equals(null)).Ignore();
        desc.Field(t => t.Equals(null)).Ignore(false);

        // assert
        Assert.Collection(
            descriptor.CreateConfiguration().Fields
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
            descriptor.CreateConfiguration().Fields.Select(t => t.Name),
            t => Assert.Equal("a", t));
    }

    [Fact]
    public async Task UseMiddleware()
    {
        // arrange
        var schema = SchemaBuilder.New().AddQueryType<BarType>().Create();
        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync("{ a b c}", TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public void Field_ArrayLengthExpression_Uses_ExpressionConfiguration()
    {
        // arrange
        var descriptor = new ObjectTypeDescriptor<ArrayHolder>(Context);

        // act
        IObjectTypeDescriptor<ArrayHolder> desc = descriptor;
        desc.BindFieldsExplicitly();
        desc.Field(t => t.Buffer.Length).Name("bufferLength");

        var field = descriptor.CreateConfiguration().Fields.Single(t => t.Name == "bufferLength");

        // assert
        Assert.Null(field.Member);
        Assert.NotNull(field.Expression);
        Assert.Equal(typeof(int), field.ResultType);
    }

    [Fact]
    public void Deprecated_OptionDisabled_Throws()
    {
        // arrange
        var descriptor = new ObjectTypeDescriptor<Foo>(Context);

        // act
        void Action() => descriptor.Deprecated("Use Bar.");

        // assert
        var exception = Assert.Throws<SchemaException>(Action);
        var error = Assert.Single(exception.Errors);
        Assert.Equal(
            "The object type `Foo` cannot be deprecated because "
            + "`SchemaOptions.EnableObjectDeprecation` is not enabled.",
            error.Message);
    }

    [Fact]
    public void Deprecated_OptionEnabled_SetsReason()
    {
        // arrange
        var context = DescriptorContext.Create(
            new SchemaOptions { EnableObjectDeprecation = true });
        var descriptor = new ObjectTypeDescriptor<Foo>(context);

        // act
        descriptor.Deprecated("Use Bar.");

        // assert
        Assert.Equal("Use Bar.", descriptor.CreateConfiguration().DeprecationReason);
    }

    [Fact]
    public void Deprecated_NoReason_SetsDefaultReason()
    {
        // arrange
        var context = DescriptorContext.Create(
            new SchemaOptions { EnableObjectDeprecation = true });
        var descriptor = new ObjectTypeDescriptor<Foo>(context);

        // act
        descriptor.Deprecated();

        // assert
        Assert.Equal("No longer supported.", descriptor.CreateConfiguration().DeprecationReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deprecated_EmptyReason_SetsDefaultReason(string? reason)
    {
        // arrange
        var context = DescriptorContext.Create(
            new SchemaOptions { EnableObjectDeprecation = true });
        var descriptor = new ObjectTypeDescriptor<Foo>(context);

        // act
        descriptor.Deprecated(reason);

        // assert
        Assert.Equal("No longer supported.", descriptor.CreateConfiguration().DeprecationReason);
    }

    [Fact]
    public void Deprecated_ClassHasGraphQLDeprecatedAttribute_SetsReason()
    {
        // arrange
        var context = DescriptorContext.Create(
            new SchemaOptions { EnableObjectDeprecation = true });

        // act
        var descriptor = new ObjectTypeDescriptor<Foo3>(context);

        // assert
        Assert.Equal("Use Bar.", descriptor.CreateConfiguration().DeprecationReason);
    }

    [Fact]
    public void Deprecated_ClassHasObsoleteAttribute_DoesNotSetReason()
    {
        // arrange
        var context = DescriptorContext.Create(
            new SchemaOptions { EnableObjectDeprecation = true });

        // act
#pragma warning disable CS0618 // Type is obsolete; obsolescence is the scenario under test.
        var descriptor = new ObjectTypeDescriptor<Foo4>(context);
#pragma warning restore CS0618

        // assert
        Assert.Null(descriptor.CreateConfiguration().DeprecationReason);
    }

    [Fact]
    public void Deprecated_ClassHasGraphQLDeprecatedAttribute_OptionDisabled_DoesNotSetReason()
    {
        // arrange & act
        var descriptor = new ObjectTypeDescriptor<Foo3>(Context);

        // assert
        Assert.Null(descriptor.CreateConfiguration().DeprecationReason);
    }

    [Fact]
    public void Deprecated_NonGenericSchemaTypeHasGraphQLDeprecatedAttribute_SetsReason()
    {
        // arrange
        var context = DescriptorContext.Create(
            new SchemaOptions { EnableObjectDeprecation = true });

        // act
        var descriptor = ObjectTypeDescriptor.FromSchemaType(context, typeof(FooType));

        // assert
        Assert.Equal("Use Bar.", descriptor.CreateConfiguration().DeprecationReason);
    }

    public class Foo : FooBase
    {
        public required string A { get; set; }
        public override required string B { get; set; }
        public required string C { get; set; }

        public override bool Equals(object? obj) => true;

        public override int GetHashCode() => 0;
    }

    [GraphQLName("FooAttr")]
    public class Foo2 : FooBase;

    [GraphQLDeprecated("Use Bar.")]
    public class Foo3
    {
        public string? Name => null;
    }

    [Obsolete("Use Bar.")]
    public class Foo4
    {
        public string? Name => null;
    }

    [GraphQLDeprecated("Use Bar.")]
    public class FooType : ObjectType;

    public class FooBase
    {
        public virtual required string B { get; set; }
    }

    public class ArrayHolder
    {
        public byte[] Buffer { get; set; } = [];
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
        private readonly FieldDelegate _next;

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
