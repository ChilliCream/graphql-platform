using System.Linq;
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
            public IQueryable<Foo> Foos { get; set; }
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class IHasFoos
        {
            [UsePaging]
            IQueryable<Foo> Foos { get; }
        }
    }
}
