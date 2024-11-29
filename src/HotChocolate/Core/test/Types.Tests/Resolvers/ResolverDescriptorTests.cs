using HotChocolate.Resolvers.Expressions;

namespace HotChocolate.Resolvers;

public class ResolverDescriptorTests
{
    [Fact]
    public void Create_With_ResolverType_Object()
    {
        var descriptor = new ResolverDescriptor(
            typeof(string),
            new FieldMember("a", "b",  typeof(object).GetMembers().First()),
            resolverType: typeof(object));

        Assert.Equal(typeof(string), descriptor.SourceType);
        Assert.Null(descriptor.ResolverType);
        Assert.NotNull(descriptor.Field.Member);
        Assert.Equal("a", descriptor.Field.TypeName);
        Assert.Equal("b", descriptor.Field.FieldName);
    }

    [Fact]
    public void Create_With_ResolverType_Null()
    {
        var descriptor = new ResolverDescriptor(
            typeof(string),
            new FieldMember("a", "b",  typeof(object).GetMembers().First()));

        Assert.Equal(typeof(string), descriptor.SourceType);
        Assert.Null(descriptor.ResolverType);
        Assert.NotNull(descriptor.Field.Member);
        Assert.Equal("a", descriptor.Field.TypeName);
        Assert.Equal("b", descriptor.Field.FieldName);
    }

    [Fact]
    public void Create_With_ResolverType_Int()
    {
        var descriptor = new ResolverDescriptor(
            typeof(string),
            new FieldMember("a", "b",  typeof(object).GetMembers().First()),
            resolverType: typeof(int));

        Assert.Equal(typeof(string), descriptor.SourceType);
        Assert.Equal(typeof(int), descriptor.ResolverType);
        Assert.NotNull(descriptor.Field.Member);
        Assert.Equal("a", descriptor.Field.TypeName);
        Assert.Equal("b", descriptor.Field.FieldName);
    }
}
