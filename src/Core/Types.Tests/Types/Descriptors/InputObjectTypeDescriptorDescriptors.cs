using Xunit;

namespace HotChocolate.Types
{
    public class InputObjectTypeDescriptorDescriptors
    {
        [Fact]
        public void Field_Ignore_PropertyIsExcluded()
        {
            // arrange
            var descriptor = new InputObjectTypeDescriptor<SimpleInput>();
            IInputObjectTypeDescriptor<SimpleInput> descriptorItf = descriptor;

            // act
            descriptorItf.Field(t => t.Id).Ignore();

            // assert
            InputObjectTypeDescription description =
                descriptor.CreateDescription();

            Assert.Collection(description.Fields,
                t => Assert.Equal("name", t.Name));
        }
    }
}
