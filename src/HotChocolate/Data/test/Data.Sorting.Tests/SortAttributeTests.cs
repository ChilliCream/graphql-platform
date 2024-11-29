using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Sorting;

public class SortAttributeTests
{
    [Fact]
    public void Create_Schema_With_SortType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query1>()
            .AddSorting()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void Create_Schema_With_SortType_With_Fluent_API()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query2>()
            .AddSorting()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void Create_Schema_With_SortType_With_Fluent_API_Ctor_Param()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query3>()
            .AddSorting()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void Create_Schema_With_SortAttributes()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query4>()
            .AddSorting()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void Create_Schema_With_GenericSortAttributes()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query5>()
            .AddSorting()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    public class Query5
    {
        [UseSorting<FooSortType>]
        public IEnumerable<Foo> Foos { get; } = new[]
        {
            new Foo { Bar = "aa", Baz = 1, Qux = 1, },
            new Foo { Bar = "ba", Baz = 1, },
            new Foo { Bar = "ca", Baz = 2, },
            new Foo { Bar = "ab", Baz = 2, },
            new Foo { Bar = "ac", Baz = 2, },
            new Foo { Bar = "ad", Baz = 2, },
            new Foo { Bar = null!, Baz = 0, },
        };
    }

    public class Query1
    {
        [UseSorting]
        public IEnumerable<Foo> Foos { get; } = new[]
        {
            new Foo { Bar = "aa", Baz = 1, Qux = 1, },
            new Foo { Bar = "ba", Baz = 1, },
            new Foo { Bar = "ca", Baz = 2, },
            new Foo { Bar = "ab", Baz = 2, },
            new Foo { Bar = "ac", Baz = 2, },
            new Foo { Bar = "ad", Baz = 2, },
            new Foo { Bar = null!, Baz = 0, },
        };
    }

    public class Query2
    {
        [UseSorting(Type = typeof(FooSortType))]
        public IEnumerable<Foo> Foos { get; } = new[]
        {
            new Foo { Bar = "aa", Baz = 1, Qux = 1, },
            new Foo { Bar = "ba", Baz = 1, },
            new Foo { Bar = "ca", Baz = 2, },
            new Foo { Bar = "ab", Baz = 2, },
            new Foo { Bar = "ac", Baz = 2, },
            new Foo { Bar = "ad", Baz = 2, },
            new Foo { Bar = null!, Baz = 0, },
        };
    }

    public class Query3
    {
        [UseSorting(typeof(FooSortType))]
        public IEnumerable<Foo> Foos { get; } = new[]
        {
            new Foo { Bar = "aa", Baz = 1, Qux = 1, },
            new Foo { Bar = "ba", Baz = 1, },
            new Foo { Bar = "ca", Baz = 2, },
            new Foo { Bar = "ab", Baz = 2, },
            new Foo { Bar = "ac", Baz = 2, },
            new Foo { Bar = "ad", Baz = 2, },
            new Foo { Bar = null!, Baz = 0, },
        };
    }

    public class Query4
    {
        [UseSorting]
        public IEnumerable<Bar> Bars { get; } = new[]
        {
            new Bar { Baz = 1, },
            new Bar { Baz = 2, },
            new Bar { Baz = 2, },
            new Bar { Baz = 2, },
            new Bar { Baz = 2, },
        };
    }

    public class FooSortType : SortInputType<Foo>
    {
        protected override void Configure(ISortInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.BindFieldsExplicitly().Field(m => m.Bar);
        }
    }

    public class Foo
    {
        public string Bar { get; set; } = default!;

        [GraphQLType(typeof(NonNullType<IntType>))]
        public long Baz { get; set; }

        [GraphQLType(typeof(IntType))]
        public int? Qux { get; set; }
    }

    [SortTest]
    public class Bar
    {
        public long Baz { get; set; }

        [IgnoreSortField]
        public int? ShouldNotBeVisible { get; set; }
    }

    public class SortTestAttribute : SortInputTypeDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            ISortInputTypeDescriptor descriptor,
            Type type)
        {
            descriptor.Name("ItWorks");
        }
    }

    public class IgnoreSortFieldAttribute : SortFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            ISortFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.Ignore();
        }
    }
}
