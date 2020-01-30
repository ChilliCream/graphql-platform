using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class RelaySchemaTests
    {
        [Fact]
        public void EnableRelay_Node_Field_On_Query_Exists()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .EnableRelaySupport()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class QueryType
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Field("some").Type<SomeType>().Resolver(new object());
            }
        }

        public class SomeType
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Some")
                    .AsNode()
                    .NodeResolver((context, id) => Task.FromResult(new object()));
                descriptor.Field("id").Type<NonNullType<IdType>>().Resolver("bar");
            }
        }
    }
}
