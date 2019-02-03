using System;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Types;
using Moq;
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

        [Fact]
        public void Build_NoExecutors_InvalidOperationException()
        {
            // arrange
            // act
            Action action = () => StitchingContextBuilder.New().Build();

            // assert
            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void AddExecuter_1_RemoteExecutorBuilderNull_ArgNullException()
        {
            // arrange
            // act
            Action action = () => StitchingContextBuilder.New()
                .AddExecutor(default(RemoteExecutorBuilder));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddExecuter_2_SchemaNullNull_ArgumentException()
        {
            // arrange
            var executor = new Mock<IQueryExecutor>();

            // act
            Action action = () => StitchingContextBuilder.New()
                .AddExecutor(null, executor.Object);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddExecuter_2_ExecutorNull_ArgumentNullException()
        {
            // arrange
            var executor = new Mock<IQueryExecutor>();

            // act
            Action action = () => StitchingContextBuilder.New()
                .AddExecutor(null, executor.Object);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddExecuter_3_BuilderNull_ArgumentNullException()
        {
            // arrange
            var executor = new Mock<IQueryExecutor>();

            // act
            Action action = () => StitchingContextBuilder.New()
                .AddExecutor
                (
                    default(Func<RemoteExecutorBuilder, RemoteExecutorBuilder>)
                );

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
