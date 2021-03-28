using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class ForbiddenRuntimeTypeTests
    {
        [Fact]
        public async Task Executable_NotAllowed()
        {
            async Task SchemaError() =>
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<Query1>()
                    .BuildSchemaAsync();

            SchemaException exception = await Assert.ThrowsAsync<SchemaException>(SchemaError);
            exception.Errors[0].Message.MatchSnapshot();
        }

        [Fact]
        public async Task ExecutableList_NotAllowed()
        {
            async Task SchemaError() =>
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<Query2>()
                    .BuildSchemaAsync();

            SchemaException exception = await Assert.ThrowsAsync<SchemaException>(SchemaError);
            exception.Errors[0].Message.MatchSnapshot();
        }

        public class Query1
        {
            public IExecutable Executable() => throw new InvalidOperationException();
        }

        public class Query2
        {
            public IExecutable[] Executable() => throw new InvalidOperationException();
        }
    }
}
