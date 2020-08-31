using System.Collections.Generic;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    public class SortingAttributeTests
    {
        [Fact]
        public void Use_Attribute_Without_SortType()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query1>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Use_Attribute_With_SortType()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query2>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class Query1
        {
            [UseSorting]
            public IEnumerable<Model> Models { get; } = new List<Model>
            {
                new Model { Foo = "Abc", Bar = 1 },
                new Model { Foo = "Abc", Bar = 2 }
            };
        }

        public class Query2
        {
            [UseSorting(SortType = typeof(ModelSortType))]
            public IEnumerable<Model> Models { get; } = new List<Model>
            {
                new Model { Foo = "Abc", Bar = 1 },
                new Model { Foo = "Abc", Bar = 2 }
            };
        }

        public class ModelSortType : SortInputType<Model>
        {
            protected override void Configure(ISortInputTypeDescriptor<Model> descriptor)
            {
                descriptor.BindFieldsExplicitly().Sortable(t => t.Bar);
            }
        }

        public class Model
        {
            public string Foo { get; set; }

            public int Bar { get; set; }
        }
    }
}