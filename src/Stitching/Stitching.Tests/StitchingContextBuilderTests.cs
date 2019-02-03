using System;
using ChilliCream.Testing;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Stitching
{
    public class StitchingContextBuilderTests
    {
        [Fact]
        public void CreateContext()
        {
            // arrange
            // act
            IStitchingContext context = StitchingContextBuilder.New()
                .AddExecutor(RemoteExecutorBuilder.New()
                    .SetSchemaName("Contract")
                    .SetSchema(FileResource.Open("Contract.graphql"))
                    .AddScalarType<DateTimeType>())
                .AddExecutor(RemoteExecutorBuilder.New()
                    .SetSchemaName("customer")
                    .SetSchema(FileResource.Open("Customer.graphql")))
                .Build();

            // assert
            Assert.NotNull(context.GetRemoteQueryClient("Contract"));
            Assert.Throws<ArgumentException>(
                () => context.GetRemoteQueryClient("Foo"));
        }
    }
}
