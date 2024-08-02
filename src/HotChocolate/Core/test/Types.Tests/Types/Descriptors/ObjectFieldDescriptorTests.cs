using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Introspection;
using Moq;

namespace HotChocolate.Types;

public class ObjectFieldDescriptorTests : DescriptorTestBase
{
    [Fact]
    public void SetGenericType()
    {
        // arrange
        var descriptor =
            ObjectFieldDescriptor.New(Context, "field");

        // act
        descriptor.Type<StringType>();

        // assert
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
        Assert.Equal(
            typeof(StringType),
            Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
    }

    [Fact]
    public void SetNonGenericType()
    {
        // arrange
        var descriptor =
            ObjectFieldDescriptor.New(Context, "field");

        // act
        descriptor.Type(typeof(StringType));

        // assert
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
        Assert.Equal(
            typeof(StringType),
            Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
    }

    [Fact]
    public void DotNetTypesDoNotOverwriteSchemaTypes()
    {
        // arrange
        var descriptor =
            ObjectFieldDescriptor.New(Context, "field");

        // act
        descriptor
            .Type<ListType<StringType>>()
            .Type<NativeType<IReadOnlyDictionary<string, string>>>();

        // assert
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
        Assert.Equal(typeof(ListType<StringType>),
            Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
    }

    [Fact]
    public void SchemaTypesOverwriteDotNetTypes()
    {
        // arrange
        var descriptor =
            ObjectFieldDescriptor.New(Context, "field");

        // act
        descriptor
            .Type<NativeType<IReadOnlyDictionary<string, string>>>()
            .Type<ListType<StringType>>();

        // assert
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
        Assert.Equal(typeof(ListType<StringType>),
            Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
    }

    [Fact]
    public void ResolverTypesDoNotOverwriteSchemaTypes()
    {
        // arrange
        var descriptor = ObjectFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments")!,
            typeof(ObjectField));

        // act
        descriptor
            .Name("args")
            .Type<NonNullType<ListType<NonNullType<__InputValue>>>>()
            .Resolve(c => c.Parent<ObjectField>().Arguments);

        // assert
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
        Assert.Equal(
            typeof(NonNullType<ListType<NonNullType<__InputValue>>>),
            Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
    }

    [Fact]
    public void OverwriteName()
    {
        // arrange
        var descriptor = ObjectFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments")!,
            typeof(ObjectField));

        // act
        descriptor.Name("args");

        // assert
        Assert.Equal("args", descriptor.CreateDefinition().Name);
    }

    [Fact]
    public void SetDescription()
    {
        // arrange
        var expectedDescription = Guid.NewGuid().ToString();
        var descriptor = ObjectFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments")!,
            typeof(ObjectField));

        // act
        descriptor.Description(expectedDescription);

        // assert
        Assert.Equal(expectedDescription,
            descriptor.CreateDefinition().Description);
    }

    [Fact]
    public async Task SetResolverAndInferTypeFromResolver()
    {
        // arrange
        var descriptor =
            ObjectFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments")!,
                typeof(ObjectField));

        // act
        descriptor.Resolve(() => "ThisIsAString");

        // assert
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
        Assert.Equal(
            typeof(string),
            Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);

        Assert.NotNull(description.Resolver);

        var context = new Mock<IResolverContext>(MockBehavior.Strict);
        Assert.Equal("ThisIsAString", await description.Resolver(context.Object));
    }

    [Fact]
    public void SetResolverAndInferTypeIsAlwaysRecognisedAsDotNetType()
    {
        // arrange
        var descriptor =
            ObjectFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments")!,
                typeof(ObjectField));

        // act
        descriptor
            .Type<__Type>()
            .Resolve(ctx => ctx.Schema
                .GetType<INamedType>(ctx.ArgumentValue<string>("type")));

        // assert
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
        Assert.Equal(
            typeof(__Type),
            Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
        Assert.NotNull(description.Resolver);
    }

    [Fact]
    public void Type_Syntax_Type_Null()
    {
        void Error() => ObjectFieldDescriptor.New(Context, "foo").Type((string)null);
        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Type_Syntax_Descriptor_Null()
    {
        void Error() => default(IObjectFieldDescriptor).Type("foo");
        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void ResolverTypeIsSet()
    {
        // arrange
        // act
        var descriptor =
            ObjectFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments")!,
                typeof(ObjectField),
                typeof(string));

        // assert
        var description = descriptor.CreateDefinition();
        Assert.Equal(typeof(string), description.ResolverType);
    }
}
