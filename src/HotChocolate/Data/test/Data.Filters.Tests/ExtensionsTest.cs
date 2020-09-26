using System;
using System.Linq;
using HotChocolate.Data.Filters;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Tests
{
    public class ExtensionTests
    {
        [Fact]
        public void Convention_DefaultScope_Extensions()
        {
            // arrange
            // act
            var convention = new FilterConvention(
                x => x.UseMock()
                    .Configure<StringOperationFilterInput>(
                        y => y.Operation(DefaultOperations.Like).Type<StringType>())
                    .Operation(DefaultOperations.Like)
                    .Name("like"));

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<IFilterConvention>(convention)
                .AddTypeInterceptor<FilterTypeInterceptor>()
                .AddQueryType(
                    c =>
                        c.Name("Query")
                            .Field("foo")
                            .Type<StringType>()
                            .Resolver("bar")
                            .Argument("test", x => x.Type<TestFilter>()));

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectField_UseFiltering()
        {
            // arrange
            // act
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddFiltering()
                .AddQueryType<Query>(
                    c =>
                        c.Field(x => x.GetFoos()).UseFiltering());

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectField_UseFiltering_Generic_RuntimeType()
        {
            // arrange
            // act
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddFiltering()
                .AddQueryType<Query>(
                    c =>
                        c.Field(x => x.GetFoos()).UseFiltering<Bar>());

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectField_UseFiltering_Generic_SchemaType()
        {
            // arrange
            // act
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddFiltering()
                .AddQueryType<Query>(
                    c =>
                        c.Field(x => x.GetFoos()).UseFiltering<BarFilterType>());

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectField_UseFiltering_Type_RuntimeType()
        {
            // arrange
            // act
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddFiltering()
                .AddQueryType<Query>(
                    c =>
                        c.Field(x => x.GetFoos()).UseFiltering(typeof(Bar)));

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectField_UseFiltering_Type_SchemaType()
        {
            // arrange
            // act
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddFiltering()
                .AddQueryType<Query>(
                    c =>
                        c.Field(x => x.GetFoos()).UseFiltering(typeof(BarFilterType)));

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectField_UseFiltering_Descriptor()
        {
            // arrange
            // act
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddFiltering()
                .AddQueryType<Query>(
                    c =>
                        c.Field(x => x.GetFoos())
                            .UseFiltering<Bar>(
                                x => x.Name("foo").Field(x => x.Foo)));

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class TestFilter : FilterInputType
        {
            protected override void Configure(IFilterInputTypeDescriptor descriptor)
            {
                descriptor.Field("test").Type<StringOperationFilterInput>();
            }
        }

        public class BarFilterType : FilterInputType<Bar>
        {
            protected override void Configure(IFilterInputTypeDescriptor<Bar> descriptor)
            {
                descriptor.BindFieldsExplicitly().Field(m => m.Foo);
            }
        }

        public class Foo
        {
            public string Bar { get; set; } = default!;
        }

        public class Bar
        {
            public string Foo { get; set; } = default!;
        }

        public class Query
        {
            public IQueryable<Foo> GetFoos() => throw new InvalidOperationException();
        }
    }
}
