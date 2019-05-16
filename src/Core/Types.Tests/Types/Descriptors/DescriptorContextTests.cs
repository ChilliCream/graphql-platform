using System;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Descriptors
{
    public class DescriptorContextTests
    {
        [Fact]
        public void Create_ServicesNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () => DescriptorContext.Create(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_With_Custom_NamingConventions()
        {
            // arrange
            var naming = new DefaultNamingConventions();
            var services = new DictionaryServiceProvider(
                typeof(INamingConventions),
                naming);

            // act
            DescriptorContext context = DescriptorContext.Create(services);

            // assert
            Assert.Equal(naming, context.Naming);
            Assert.NotNull(context.Inspector);
        }

        [Fact]
        public void Create_With_Custom_TypeInspector()
        {
            // arrange
            var inspector = new DefaultTypeInspector();
            var services = new DictionaryServiceProvider(
                typeof(ITypeInspector),
                inspector);

            // act
            DescriptorContext context = DescriptorContext.Create(services);

            // assert
            Assert.Equal(inspector, context.Inspector);
            Assert.NotNull(context.Naming);
        }

        [Fact]
        public void Create_Without_Services()
        {
            // arrange
            // act
            DescriptorContext context = DescriptorContext.Create();

            // assert
            Assert.NotNull(context.Naming);
            Assert.NotNull(context.Inspector);
        }
    }
}
