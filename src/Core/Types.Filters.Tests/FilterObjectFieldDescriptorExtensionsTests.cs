using System;
using Moq;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class FilterObjectFieldDescriptorExtensionsTests
    {
        [Fact]
        public void UseFiltering_FieldDescConfig_FieldDesc_Is_Null()
        {
            // arrange
            Action<IFilterInputTypeDescriptor<object>> config = d => { };

            // act
            Action action = () =>
                FilterObjectFieldDescriptorExtensions
                    .UseFiltering<object>(null, config);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void UseFiltering_FieldDescConfig_Config_Is_Null()
        {
            // arrange
            IObjectFieldDescriptor desc = Mock.Of<IObjectFieldDescriptor>();

            // act
            Action action = () =>
                FilterObjectFieldDescriptorExtensions
                    .UseFiltering<object>(desc, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
