using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class UsePagingAttributeTests
    {
        [Fact]
        public void UsePagingAttribute_Infer_Types()
        {
            SchemaBuilder.New()
                .AddQueryType<Query>()
                .Create()
                .ToString()
                .MatchSnapshot();
        }

        [Fact]
        public void UsePagingAttribute_Execute_Query()
        {
            SchemaBuilder.New()
                .AddQueryType<Query>()
                .Create()
                .MakeExecutable()
                .Execute("{ foos(first: 1) { nodes { bar } } }")
                .MatchSnapshot();
        }

        [Fact]
        public void UsePagingAttribute_Infer_Types_On_Interface()
        {
            SchemaBuilder.New()
                .AddType<IHasFoos>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create()
                .ToString()
                .MatchSnapshot();
        }

        public class Query
        {
            [UsePaging]
            public IQueryable<Foo> Foos { get; set; } = new List<Foo>
            {
                new Foo { Bar = "first" },
                new Foo { Bar = "second" },
            }.AsQueryable();
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public interface IHasFoos
        {
            [UsePaging]
            IQueryable<Foo> Foos { get; }
        }
    }
}
