using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public class EnumTypeDescriptorTests : DescriptorTestBase
{
    [Fact]
    public void InferNameFromType()
    {
        // act
        var descriptor = EnumTypeDescriptor.New(Context, typeof(FooEnum));

        // assert
        Assert.Equal("FooEnum", descriptor.CreateDefinition().Name);
    }

    [Fact]
    public void NoTypeProvided()
    {
        // act
        Action a = () => EnumTypeDescriptor.New(Context, (Type)null);

        // assert
        Assert.Throws<ArgumentNullException>(a);
    }

    [Fact]
    public void InferValuesFromType()
    {
        // act
        var descriptor = EnumTypeDescriptor.New(Context, typeof(FooEnum));

        // assert
        var description = descriptor.CreateDefinition();
        Assert.Collection(description.Values,
            t =>
            {
                Assert.Equal("BAR1", t.Name);
                Assert.Equal(FooEnum.Bar1, t.RuntimeValue);
            },
            t =>
            {
                Assert.Equal("BAR2", t.Name);
                Assert.Equal(FooEnum.Bar2, t.RuntimeValue);
            });
    }

    [Fact]
    public void SpecifyOneValueInferTheOthers()
    {
        // arrange
        var descriptor = EnumTypeDescriptor.New(Context, typeof(FooEnum));

        // act
        IEnumTypeDescriptor desc = descriptor;
        desc.Value(FooEnum.Bar1).Name("FOOBAR");

        // assert
        var description = descriptor.CreateDefinition();
        Assert.Collection(description.Values,
            t =>
            {
                Assert.Equal("FOOBAR", t.Name);
                Assert.Equal(FooEnum.Bar1, t.RuntimeValue);
            },
            t =>
            {
                Assert.Equal("BAR2", t.Name);
                Assert.Equal(FooEnum.Bar2, t.RuntimeValue);
            });
    }

    [Fact]
    public void ExplicitValueBinding()
    {
        // arrange
        var descriptor = EnumTypeDescriptor.New(Context, typeof(FooEnum));

        // act
        IEnumTypeDescriptor desc = descriptor;
        desc.Value(FooEnum.Bar1).Name("FOOBAR");
        desc.BindValues(BindingBehavior.Explicit);

        // assert
        var description = descriptor.CreateDefinition();
        Assert.Collection(description.Values,
            t =>
            {
                Assert.Equal("FOOBAR", t.Name);
                Assert.Equal(FooEnum.Bar1, t.RuntimeValue);
            });
    }

    [Fact]
    public void AddDirective()
    {
        // arrange
        var descriptor = EnumTypeDescriptor.New(Context);

        // act
        IEnumTypeDescriptor desc = descriptor;
        desc.Directive("Bar");

        // assert
        var description = descriptor.CreateDefinition();
        Assert.Collection(
            description.Directives,
            t => Assert.Equal("Bar", Assert.IsType<DirectiveNode>(t.Value).Name.Value));
    }

    [Fact]
    public void AddDirectiveWithDirectiveNode()
    {
        // arrange
        var descriptor = EnumTypeDescriptor.New(Context);

        // act
        IEnumTypeDescriptor desc = descriptor;
        desc.Directive(new DirectiveNode("Bar"));

        // assert
        var description = descriptor.CreateDefinition();
        Assert.Collection(description.Directives,
            t => Assert.Equal("Bar", Assert.IsType<DirectiveNode>(t.Value).Name.Value));
    }

    [Fact]
    public void AddDirectiveWithArgument()
    {
        // arrange
        var descriptor = EnumTypeDescriptor.New(Context);

        // act
        IEnumTypeDescriptor desc = descriptor;
        desc.Directive("Bar",
            new ArgumentNode("a", new StringValueNode("b")));

        // assert
        var description = descriptor.CreateDefinition();
        Assert.Collection(description.Directives,
            t =>
            {
                Assert.Equal("Bar", Assert.IsType<DirectiveNode>(t.Value).Name.Value);
                Assert.Collection(
                    Assert.IsType<DirectiveNode>(t.Value).Arguments,
                    x =>
                    {
                        Assert.Equal("a", x.Name.Value);
                        Assert.IsType<StringValueNode>(x.Value);
                        Assert.Equal("b", ((StringValueNode)x.Value).Value);
                    });
            });
    }

    private enum FooEnum
    {
        Bar1,
        Bar2,
    }
}
