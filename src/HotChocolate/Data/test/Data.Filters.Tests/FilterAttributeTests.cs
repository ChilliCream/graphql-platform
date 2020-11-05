using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class FilterAttributeTests
    {
        [Fact]
        public void Create_Schema_With_FilterType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query1>()
                .AddFiltering()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Schema_With_FilterType_With_Fluent_API_Ctor_Param()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query3>()
                .AddFiltering()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Schema_With_FilterType_With_Fluent_API()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query2>()
                .AddFiltering()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Schema_With_FilterAttributes()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query4>()
                .AddFiltering()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class Query1
        {
            [UseFiltering]
            public IEnumerable<Foo> Foos { get; } = new[]
            {
                new Foo { Bar = "aa", Baz = 1, Qux = 1 },
                new Foo { Bar = "ba", Baz = 1 },
                new Foo { Bar = "ca", Baz = 2 },
                new Foo { Bar = "ab", Baz = 2 },
                new Foo { Bar = "ac", Baz = 2 },
                new Foo { Bar = "ad", Baz = 2 },
                new Foo { Bar = null!, Baz = 0 }
            };
        }

        public class Query2
        {
            [UseFiltering(Type = typeof(FooFilterType))]
            public IEnumerable<Foo> Foos { get; } = new[]
            {
                new Foo { Bar = "aa", Baz = 1, Qux = 1 },
                new Foo { Bar = "ba", Baz = 1 },
                new Foo { Bar = "ca", Baz = 2 },
                new Foo { Bar = "ab", Baz = 2 },
                new Foo { Bar = "ac", Baz = 2 },
                new Foo { Bar = "ad", Baz = 2 },
                new Foo { Bar = null!, Baz = 0 }
            };
        }

        public class Query3
        {
            [UseFiltering(typeof(FooFilterType))]
            public IEnumerable<Foo> Foos { get; } = new[]
            {
                new Foo { Bar = "aa", Baz = 1, Qux = 1 },
                new Foo { Bar = "ba", Baz = 1 },
                new Foo { Bar = "ca", Baz = 2 },
                new Foo { Bar = "ab", Baz = 2 },
                new Foo { Bar = "ac", Baz = 2 },
                new Foo { Bar = "ad", Baz = 2 },
                new Foo { Bar = null!, Baz = 0 }
            };
        }

        public class Query4
        {
            [UseFiltering]
            public IEnumerable<Bar> Bars { get; } = new[]
            {
                new Bar { Baz = 1 },
                new Bar { Baz = 2 },
                new Bar { Baz = 2 },
                new Bar { Baz = 2 },
                new Bar { Baz = 2 },
            };
        }

        public class FooFilterType : FilterInputType<Foo>
        {
            protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.BindFieldsExplicitly().Field(m => m.Bar);
            }
        }

        public class Foo
        {
            public string Bar { get; set; }

            [GraphQLType(typeof(NonNullType<IntType>))]
            public long Baz { get; set; }

            [GraphQLType(typeof(IntType))]
            public int? Qux { get; set; }

            public Foo? Nested { get; set; }
        }

        [FilterTest]
        public class Bar
        {
            public long Baz { get; set; }

            [IgnoreFilterField]
            public int? ShouldNotBeVisible { get; set; }
        }

        public class FilterTestAttribute : FilterInputTypeDescriptorAttribute
        {
            public override void OnConfigure(
                IDescriptorContext context,
                IFilterInputTypeDescriptor descriptor,
                Type type)
            {
                descriptor.Name("ItWorks");
            }
        }

        public class IgnoreFilterFieldAttribute : FilterFieldDescriptorAttribute
        {
            public override void OnConfigure(
                IDescriptorContext context,
                IFilterFieldDescriptor descriptor,
                MemberInfo member)
            {
                descriptor.Ignore();
            }
        }
    }
}
