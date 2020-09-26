using System;
using System.Collections.Generic;
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
            var conventions = new Dictionary<(Type, string), CreateConvention>();

            // act
            Action action = () => DescriptorContext.Create(
                options, null, conventions, new Dictionary<string, object>(),
                new SchemaBuilder.LazySchema());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_OptionsNull_ArgumentException()
        {
            // arrange
            var service = new EmptyServiceProvider();
            var conventions = new Dictionary<(Type, string), CreateConvention>();

            // act
            Action action = () => DescriptorContext.Create(
                null, service, conventions, new Dictionary<string, object>(),
                new SchemaBuilder.LazySchema());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_ConventionsNull_ArgumentException()
        {
            // arrange
            var service = new EmptyServiceProvider();
            var options = new SchemaOptions();

            // act
            Action action = () => DescriptorContext.Create(
                options, service, null, new Dictionary<string, object>(),
                new SchemaBuilder.LazySchema());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_With_Custom_NamingConventions()
        {
            // arrange
            var options = new SchemaOptions();
            var naming = new DefaultNamingConventions();
            var conventions = new Dictionary<(Type, string), CreateConvention>();
            var services = new DictionaryServiceProvider(
                typeof(INamingConventions),
                naming);

            // act
            var context = DescriptorContext.Create(
                options, services, conventions, new Dictionary<string, object>(),
                new SchemaBuilder.LazySchema());

            // assert
            Assert.Equal(naming, context.Naming);
            Assert.NotNull(context.TypeInspector);
            Assert.Equal(options, context.Options);
        }


        [Fact]
        public void Create_With_Custom_NamingConventions_AsIConvention()
        {
            // arrange
            var options = new SchemaOptions();
            var naming = new DefaultNamingConventions();
            var conventions = new Dictionary<(Type, string), CreateConvention>
            {
                { (typeof(INamingConventions), null), s => naming }
            };

            // act
            var context = DescriptorContext.Create(
                options, new EmptyServiceProvider(), conventions, new Dictionary<string, object>(),
                new SchemaBuilder.LazySchema());

            // assert
            Assert.Equal(naming, context.Naming);
            Assert.NotNull(context.TypeInspector);
            Assert.Equal(options, context.Options);
        }

        [Fact]
        public void Create_With_Custom_TypeInspector()
        {
            // arrange
            var options = new SchemaOptions();
            var inspector = new DefaultTypeInspector();
            var conventions = new Dictionary<(Type, string), CreateConvention>();
            var services = new DictionaryServiceProvider(
                typeof(ITypeInspector),
                inspector);

            // act
            var context = DescriptorContext.Create(
                options, services, conventions, new Dictionary<string, object>(),
                new SchemaBuilder.LazySchema());

            // assert
            Assert.Equal(inspector, context.TypeInspector);
            Assert.NotNull(context.Naming);
            Assert.Equal(options, context.Options);
        }

        [Fact]
        public void Create_Without_Services()
        {
            // arrange
            // act
            var context = DescriptorContext.Create();

            // assert
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Naming);
            Assert.NotNull(context.TypeInspector);
        }

        private class Convention : Descriptors.Convention
        {
            public static Convention Default { get; } = new Convention();
        }
    }
}
