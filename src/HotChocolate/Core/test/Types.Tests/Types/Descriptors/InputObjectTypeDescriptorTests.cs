using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public class InputObjectTypeDescriptorTests
    : DescriptorTestBase
{
    [Fact]
    public void Field_Ignore_PropertyIsExcluded()
    {
        // arrange
        var descriptor =
            InputObjectTypeDescriptor.New<SimpleInput>(Context);

        // act
        descriptor.Field(t => t.Id).Ignore();

        // assert
        var description =
            descriptor.CreateDefinition();

        Assert.Collection(description.Fields,
            t => Assert.Equal("name", t.Name));
    }

    [Fact]
    public void Field_Unignore_PropertyIsExcluded()
    {
        // arrange
        var descriptor =
            InputObjectTypeDescriptor.New<SimpleInput>(Context);

        // act
        descriptor.Field(t => t.Id).Ignore();
        descriptor.Field(t => t.Id).Ignore(false);

        // assert
        var description =
            descriptor.CreateDefinition();

        Assert.Collection(description.Fields,
            t => Assert.Equal("id", t.Name),
            t => Assert.Equal("name", t.Name));
    }

    public class SimpleInput
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
