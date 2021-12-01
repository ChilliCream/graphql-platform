using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class SchemaTransformationTests
    {
        [Fact]
        public async Task Schema_Should_BeGeneratedCorrectly_When_ThrowIsSpecified()
        {
            // arrange
            // act
            ISchema schema = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .EnableMutationConventions()
                .BuildSchemaAsync();

            // assert
            schema.Print().MatchSnapshot();
        }
    }

    public class CustomException : Exception
    {
    }

    public class Foo
    {
        public string Bar { get; }
    }

    public class Query
    {
        [Error(typeof(CustomException))]
        [Error(typeof(InvalidOperationException))]
        public Foo GetFoo() => null!;
    }
}
