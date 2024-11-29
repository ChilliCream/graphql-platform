using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters;

public class FilterAttributeTests
{
    [Fact]
    public void Create_Schema_With_FilterInput()
        => SchemaBuilder.New()
            .AddQueryType<Query1>()
            .AddFiltering()
            .Create()
            .MatchSnapshot();

    [Fact]
    public void Create_Schema_With_FilterInput_With_Fluent_API_Ctor_Param()
        => SchemaBuilder.New()
            .AddQueryType<Query3>()
            .AddFiltering()
            .Create()
            .MatchSnapshot();

    [Fact]
    public void Create_Schema_With_FilterInput_With_Fluent_API()
        => SchemaBuilder.New()
            .AddQueryType<Query2>()
            .AddFiltering()
            .Create()
            .MatchSnapshot();

    [Fact]
    public void Create_Schema_With_FilterAttributes()
        => SchemaBuilder.New()
            .AddQueryType<Query4>()
            .AddFiltering()
            .Create()
            .MatchSnapshot();

    [Fact]
    public void Create_Schema_With_FilterInput_With_GenericAttribute()
        => SchemaBuilder.New()
            .AddQueryType<Query5>()
            .AddFiltering()
            .Create()
            .MatchSnapshot();

    public class Query5
    {
        [UseFiltering<FooFilterInput>]
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
        [UseFiltering]
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
        [UseFiltering(Type = typeof(FooFilterInput))]
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
        [UseFiltering(typeof(FooFilterInput))]
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
        [UseFiltering]
        public IEnumerable<Bar> Bars { get; } = new[]
        {
            new Bar { Baz = 1, },
            new Bar { Baz = 2, },
            new Bar { Baz = 2, },
            new Bar { Baz = 2, },
            new Bar { Baz = 2, },
        };
    }

    public class FooFilterInput : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
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
