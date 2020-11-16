using System;
using System.Linq;
using HotChocolate.Data.Sorting;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Tests
{
    public class ExtensionTests
    {
        [Fact]
        public void Convention_DefaultScope_Extensions_Enum()
        {
            // arrange
            // act
            var convention = new SortConvention(
                x => x.UseMock()
                    .ConfigureEnum<DefaultSortEnumType>(y => y.Operation(123))
                    .Operation(123).Name("test"));

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<ISortConvention>(convention)
                .TryAddTypeInterceptor<SortTypeInterceptor>()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar")
                        .Argument("test", x => x.Type<TestSort>()));

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_DefaultScope_Extensions()
        {
            // arrange
            // act
            var convention = new SortConvention(
                x => x.UseMock()
                    .Configure<TestSort>(
                        y => y.Field("foo").Type<DefaultSortEnumType>())
                    .Operation(123).Name("test"));

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<ISortConvention>(convention)
                .TryAddTypeInterceptor<SortTypeInterceptor>()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar")
                        .Argument("test", x => x.Type<TestSort>()));

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectField_UseSorting()
        {
            // arrange
            // act
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddSorting()
                .AddQueryType<Query>(
                    c =>
                        c.Field(x => x.GetFoos()).UseSorting());

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectField_UseSorting_Generic_RuntimeType()
        {
            // arrange
            // act
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddSorting()
                .AddQueryType<Query>(
                    c =>
                        c.Field(x => x.GetFoos()).UseSorting<Bar>());

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectField_UseSorting_Generic_SchemaType()
        {
            // arrange
            // act
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddSorting()
                .AddQueryType<Query>(
                    c =>
                        c.Field(x => x.GetFoos()).UseSorting<BarSortType>());

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectField_UseSorting_Type_RuntimeType()
        {
            // arrange
            // act
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddSorting()
                .AddQueryType<Query>(
                    c =>
                        c.Field(x => x.GetFoos()).UseSorting(typeof(Bar)));

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectField_UseSorting_Type_SchemaType()
        {
            // arrange
            // act
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddSorting()
                .AddQueryType<Query>(
                    c =>
                        c.Field(x => x.GetFoos()).UseSorting(typeof(BarSortType)));

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectField_UseSorting_Descriptor()
        {
            // arrange
            // act
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddSorting()
                .AddQueryType<Query>(
                    c =>
                        c.Field(x => x.GetFoos())
                            .UseSorting<Bar>(
                                x => x.Name("foo").Field(x => x.Foo)));

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
                    IObjectFieldDescriptor f = default;
        }

        private class TestSort : SortInputType
        {
            protected override void Configure(ISortInputTypeDescriptor descriptor)
            {
                descriptor.Field("test").Type<DefaultSortEnumType>();
            }
        }

        public class BarSortType : SortInputType<Bar>
        {
            protected override void Configure(ISortInputTypeDescriptor<Bar> descriptor)
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
