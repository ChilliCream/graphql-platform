using System.Threading.Tasks;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class SourceObjectConversionTests
    {
        [Fact]
        public async Task ConvertSourceObject()
        {
            // arrange
            bool conversionTriggered = false;

            var executor = new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .AddTypeConverter<Foo, Baz>(input => 
                {
                    conversionTriggered = true;
                    return new Baz { Qux = input.Bar };
                })
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync()
                .Result;

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ foo { qux } }");

            // assert
            Assert.True(conversionTriggered);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task NoConverter_Specified()
        {
            // arrange
            ISchema schema =
                SchemaBuilder.New()
                    .AddQueryType<QueryType>()
                    .Create();

            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo { qux } }")
                    .Create();

            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(request);

            // assert
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        public class Query
        {
            public Foo Foo { get; } = new Foo { Bar = "bar" };
        }

        public class QueryType
            : ObjectType<Query>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor.Field(t => t.Foo).Type<BazType>();
            }
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class Baz
        {
            public string Qux { get; set; }
        }

        public class BazType
            : ObjectType<Baz>
        {
        }
    }
}
