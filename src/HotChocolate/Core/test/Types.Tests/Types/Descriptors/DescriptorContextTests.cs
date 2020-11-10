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
        public void Create_With_Custom_NamingConventions()
        {
            // arrange
            var options = new SchemaOptions();
            var naming = new DefaultNamingConventions();
            var conventions = new Dictionary<(Type, string), List<CreateConvention>>();
            var services = new DictionaryServiceProvider(
                typeof(INamingConventions),
                naming);

            // act
            var context = DescriptorContext.Create(
                options,
                services,
                conventions,
                new Dictionary<string, object>(),
                new SchemaBuilder.LazySchema(),
                new AggregateSchemaInterceptor(),
                new AggregateTypeInterceptor());

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
            var conventions = new Dictionary<(Type, string), List<CreateConvention>>
            {
                { (typeof(INamingConventions), null), new List<CreateConvention>{s => naming} }
            };

            // act
            var context = DescriptorContext.Create(
                options,
                new EmptyServiceProvider(),
                conventions,
                new Dictionary<string, object>(),
                new SchemaBuilder.LazySchema(),
                new AggregateSchemaInterceptor(),
                new AggregateTypeInterceptor());

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
            var conventions = new Dictionary<(Type, string), List<CreateConvention>>();
            var services = new DictionaryServiceProvider(
                typeof(ITypeInspector),
                inspector);

            // act
            var context = DescriptorContext.Create(
                options,
                services,
                conventions,
                new Dictionary<string, object>(),
                new SchemaBuilder.LazySchema(),
                new AggregateSchemaInterceptor(),
                new AggregateTypeInterceptor());

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
