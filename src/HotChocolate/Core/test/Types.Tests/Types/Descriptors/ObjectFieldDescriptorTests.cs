using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
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
        var description = descriptor.CreateConfiguration();
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
        var description = descriptor.CreateConfiguration();
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
            .Type<NamedRuntimeType<IReadOnlyDictionary<string, string>>>();

        // assert
        var description = descriptor.CreateConfiguration();
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
            .Type<NamedRuntimeType<IReadOnlyDictionary<string, string>>>()
            .Type<ListType<StringType>>();

        // assert
        var description = descriptor.CreateConfiguration();
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
        var description = descriptor.CreateConfiguration();
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
        Assert.Equal("args", descriptor.CreateConfiguration().Name);
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
            descriptor.CreateConfiguration().Description);
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
        var description = descriptor.CreateConfiguration();
        var typeRef = description.Type;
        Assert.Equal(
            typeof(string),
            Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);

        Assert.NotNull(description.Resolver);

        var context = new Mock<IResolverContext>(MockBehavior.Strict);
        Assert.Equal("ThisIsAString", await description.Resolver(context.Object));
    }

    [Fact]
    public void SetResolverAndInferTypeIsAlwaysRecognizedAsDotNetType()
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
            .Resolve(ctx => ctx.Schema.Types[ctx.ArgumentValue<string>("type")]);

        // assert
        var description = descriptor.CreateConfiguration();
        var typeRef = description.Type;
        Assert.Equal(
            typeof(__Type),
            Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
        Assert.NotNull(description.Resolver);
    }

    [Fact]
    public void Type_Syntax_Type_Null()
    {
        void Error() => ObjectFieldDescriptor.New(Context, "foo").Type((string)null!);
        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Type_Syntax_Descriptor_Null()
    {
        void Error() => default(IObjectFieldDescriptor)!.Type("foo");
        Assert.Throws<NullReferenceException>(Error);
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
        var description = descriptor.CreateConfiguration();
        Assert.Equal(typeof(string), description.ResolverType);
    }

    [Fact]
    public void ExpressionSelectionSetFormatter_Format_PrimitiveProperty()
    {
        // arrange & act
        // When the property type is a value type (int), the compiler wraps
        // the member access in a Convert expression to box it to object.
        // The formatter must unwrap this to extract the member name.
        var result = ObjectFieldDescriptor.ExpressionSelectionSetFormatter
            .Format<ParentRequiresTestEntity, object>(e => e.Type);

        // assert
        Assert.Equal("Type", result);
    }

    [Fact]
    public void ExpressionSelectionSetFormatter_Format_EnumProperty()
    {
        // arrange & act
        var result = ObjectFieldDescriptor.ExpressionSelectionSetFormatter
            .Format<ParentRequiresTestEntity, object>(e => e.Provider);

        // assert
        Assert.Equal("Provider", result);
    }

    [Fact]
    public void ExpressionSelectionSetFormatter_Format_StringProperty()
    {
        // arrange & act
        // String is a reference type, so no Convert wrapping occurs.
        var result = ObjectFieldDescriptor.ExpressionSelectionSetFormatter
            .Format<ParentRequiresTestEntity, object>(e => e.Name);

        // assert
        Assert.Equal("Name", result);
    }

    [Fact]
    public void ExpressionSelectionSetFormatter_Format_AnonymousObject()
    {
        // arrange & act
        var result = ObjectFieldDescriptor.ExpressionSelectionSetFormatter
            .Format<ParentRequiresTestEntity, object>(e => new { e.Type, e.Provider });

        // assert
        Assert.Equal("Type Provider", result);
    }

    [Fact]
    public void ParentRequires_WithPrimitiveProperty_SetsRequirements()
    {
        // arrange
        var descriptor = ObjectFieldDescriptor.New(Context, "field");

        // act
        descriptor.ParentRequires<ParentRequiresTestEntity>(e => e.Type);

        // assert
        var config = descriptor.CreateConfiguration();
        Assert.True(config.Flags.HasFlag(CoreFieldFlags.WithRequirements));
        var feature = config.Features.Get<FieldRequirementFeature>();
        Assert.NotNull(feature);
        Assert.Equal("Type", feature.Requirements);
    }

    [Fact]
    public void ParentRequires_WithEnumProperty_SetsRequirements()
    {
        // arrange
        var descriptor = ObjectFieldDescriptor.New(Context, "field");

        // act
        descriptor.ParentRequires<ParentRequiresTestEntity>(e => e.Provider);

        // assert
        var config = descriptor.CreateConfiguration();
        Assert.True(config.Flags.HasFlag(CoreFieldFlags.WithRequirements));
        var feature = config.Features.Get<FieldRequirementFeature>();
        Assert.NotNull(feature);
        Assert.Equal("Provider", feature.Requirements);
    }

    private enum TestProvider
    {
        None,
        Google,
        Facebook
    }

    private sealed class ParentRequiresTestEntity
    {
        public int Type { get; set; }
        public TestProvider Provider { get; set; }
        public string Name { get; set; } = default!;
    }
}
