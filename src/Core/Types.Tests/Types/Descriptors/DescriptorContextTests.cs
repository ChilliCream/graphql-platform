using System;
using HotChocolate.Configuration;
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
            var options = new SchemaOptions();

            // act
            Action action = () => 
                DescriptorContext.Create(
                    options, 
                    null, 
                    () => throw new NotSupportedException());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_OptionsNull_ArgumentException()
        {
            // arrange
            var services = new EmptyServiceProvider();

            // act
            Action action = () => 
                DescriptorContext.Create(
                    null,
                    services,
                    () => throw new NotSupportedException());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_With_Custom_NamingConventions()
        {
            // arrange
            var options = new SchemaOptions();
            var naming = new DefaultNamingConventions();
            var services = new DictionaryServiceProvider(
                typeof(INamingConventions),
                naming);

            // act
            DescriptorContext context =
                DescriptorContext.Create(
                    options, 
                    services, 
                    () => throw new NotSupportedException());

            // assert
            Assert.Equal(naming, context.Naming);
            Assert.NotNull(context.Inspector);
            Assert.Equal(options, context.Options);
        }

        [Fact]
        public void Create_With_Custom_TypeInspector()
        {
            // arrange
            var options = new SchemaOptions();
            var inspector = new DefaultTypeInspector();
            var services = new DictionaryServiceProvider(
                typeof(ITypeInspector),
                inspector);

            // act
            DescriptorContext context =
                DescriptorContext.Create(
                    options, 
                    services, 
                    () => throw new NotSupportedException());

            // assert
            Assert.Equal(inspector, context.Inspector);
            Assert.NotNull(context.Naming);
            Assert.Equal(options, context.Options);
        }

        [Fact]
        public void Create_Without_Services()
        {
            // arrange
            // act
            DescriptorContext context = DescriptorContext.Create();

            // assert
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Naming);
            Assert.NotNull(context.Inspector);
        }
    }
}
