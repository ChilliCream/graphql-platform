using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.RateLimit;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.RateLimit.Tests
{
    public class LimitMiddlewareTests
    {
        [Fact]
        public async Task GivenValidLimitedQuery_WhenExecute_ShouldSuccess()
        {
            // Arrange
            ILimitContext limitContext = CreateLimitContext();
            IServiceProvider services = CreateServiceProvider(limitContext);

            // Act
            IExecutionResult result = await ExecuteVersionByUserQuery(services);

            // Assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task GivenExceededLimitedQuery_WhenExecute_ShouldFail()
        {
            // Arrange
            ILimitContext limitContext = CreateLimitContext();
            IServiceProvider services = CreateServiceProvider(limitContext);
            ILimitStore limitStore = services.GetService<ILimitStore>();
            await limitStore.SetAsync(
                "1-user", TimeSpan.FromMinutes(1), Limit.Create(DateTime.UtcNow, 1), default);

            // Act
            IExecutionResult result = await ExecuteVersionByUserQuery(services);

            // Assert
            result.MatchSnapshot();
        }

        private async Task<IExecutionResult> ExecuteVersionByUserQuery(IServiceProvider services)
        {
            IRequestExecutor executor = await services
                .GetRequestExecutorAsync();

            return await executor
                .ExecuteAsync($@"
                        query version {{
                            version
                        }}");
        }

        private ServiceProvider CreateServiceProvider(ILimitContext limitContext)
        {
            return new ServiceCollection()
                .AddDistributedMemoryCache()
                .AddRateLimit(o =>
                {
                    o.AddPolicy("subClaim", pb => pb
                        .AddClaimIdentifier("subClaim")
                        .WithLimit(TimeSpan.FromMinutes(1), 1));
                    o.AddPolicy("clientIdHeader", pb => pb
                        .AddHeaderIdentifier("Client-Id")
                        .WithLimit(TimeSpan.FromMinutes(1), 1));
                })
                .AddSingleton(limitContext)
                .AddGraphQLServer()
                .AddQueryType<QueryType>()
                .AddLimitDirectiveType()
                .Services
                .BuildServiceProvider();
        }

        private ILimitContext CreateLimitContext()
        {
            var limitContext = new Mock<ILimitContext>();

            limitContext.Setup(x => x
                    .CreateRequestIdentity(It.IsAny<IReadOnlyCollection<IPolicyIdentifier>>(), It.IsAny<Path>()))
                .Returns(RequestIdentity.Create("user", "1"));

            limitContext.Setup(x => x
                    .CreateRequestIdentity(It.IsAny<IReadOnlyCollection<IPolicyIdentifier>>(), It.IsAny<Path>()))
                .Returns(RequestIdentity.Create("user", "1"));

            return limitContext.Object;
        }

        private class QueryType : ObjectType<Query>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor
                    .Field(x => x.Version())
                    .Type<StringType>()
                    .Limit("subClaim");
            }
        }

        private class Query
        {
            public string Version() => "1.0.0";
        }
    }
}
