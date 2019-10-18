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
            var conventions = new Dictionary<Type, IConvention>();

            // act
            Action action = () => DescriptorContext.Create(options, null, conventions);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_OptionsNull_ArgumentException()
        {
            // arrange
            var service = new EmptyServiceProvider();
            var conventions = new Dictionary<Type, IConvention>();

            // act
            Action action = () => DescriptorContext.Create(null, service, conventions);

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
            Action action = () => DescriptorContext.Create(options, service, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_With_Custom_NamingConventions()
        {
            // arrange
            var options = new SchemaOptions();
            var naming = new DefaultNamingConventions();
            var conventions = new Dictionary<Type, IConvention>();
            var services = new DictionaryServiceProvider(
                typeof(INamingConventions),
                naming);

            // act
            DescriptorContext context =
                DescriptorContext.Create(options, services, conventions);

            // assert
            Assert.Equal(naming, context.Naming);
            Assert.NotNull(context.Inspector);
            Assert.Equal(options, context.Options);
        }


        [Fact]
        public void Create_With_Custom_NamingConventions_AsIConvention()
        {
            // arrange
            var options = new SchemaOptions();
            var naming = new DefaultNamingConventions();
            var conventions = new Dictionary<Type, IConvention>();
            conventions.Add(typeof(INamingConventions), naming);
            var services = new DictionaryServiceProvider();

            // act
            DescriptorContext context =
                DescriptorContext.Create(options, services, conventions);

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
            var conventions = new Dictionary<Type, IConvention>();
            var services = new DictionaryServiceProvider(
                typeof(ITypeInspector),
                inspector);

            // act
            DescriptorContext context =
                DescriptorContext.Create(options, services, conventions);

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

        [Fact]
        public void Create_Without_ServicesAndRegisteringConventions()
        {
            // arrange
            // act
            DescriptorContext context = DescriptorContext.Create();

            // assert
            Assert.False(context.TryGetConvention(out Convention convention));
            Assert.Null(convention);
            Assert.Null(context.GetConvention<Convention>());
        }

        [Fact]
        public void Create_Without_RegisteringConventions()
        {
            // arrange
            var options = new SchemaOptions();
            var inspector = new DefaultTypeInspector();
            var conventions = new Dictionary<Type, IConvention>();
            var services = new DictionaryServiceProvider(
                new KeyValuePair<Type, object>(
                    typeof(ITypeInspector),
                    inspector));
            // act
            DescriptorContext context =
                DescriptorContext.Create(options, services, conventions);

            // assert
            Assert.False(context.TryGetConvention(out Convention convention));
            Assert.Null(convention);
            Assert.Null(context.GetConvention<Convention>());
        }

        [Fact]
        public void Create_With_RegisteringConventions()
        {
            // arrange
            var options = new SchemaOptions();
            var inspector = new DefaultTypeInspector();
            var conventions = new Dictionary<Type, IConvention>();
            conventions.Add(typeof(Convention), Convention.Default);
            var services = new DictionaryServiceProvider(
                new KeyValuePair<Type, object>(
                    typeof(ITypeInspector),
                    inspector));

            // act
            DescriptorContext context =
                DescriptorContext.Create(options, services, conventions);

            // assert
            Assert.True(context.TryGetConvention(out Convention convention));
            Assert.Equal(Convention.Default, convention);
            Assert.Equal(Convention.Default, context.GetConvention<Convention>());
        }

        private class Convention : IConvention
        {
            public static Convention Default { get; } = new Convention();
        }
    }
}
