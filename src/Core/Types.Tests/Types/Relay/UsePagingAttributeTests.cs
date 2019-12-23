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

        [Fact]
        public void UsePagingAttribute_On_Extension_Infer_Types()
        {
            SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddType<QueryExtension>()
                .Create()
                .ToString()
                .MatchSnapshot();
        }

        [Fact]
        public void UsePagingAttribute_On_Extension_Execute_Query()
        {
            SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddType<QueryExtension>()
                .Create()
                .MakeExecutable()
                .Execute("{ foos(first: 1) { nodes { bar } } }")
                .MatchSnapshot();
        }

        public class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
            }
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

        [ExtendObjectType(Name = "Query")]
        public class QueryExtension : Query
        {
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
