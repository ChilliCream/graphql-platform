using System;
using HotChocolate.Execution;
using HotChocolate.Stitching.Delegation;
using Moq;
using Xunit;

namespace HotChocolate.Stitching.Delegation
{
    public class RemoteExecutorAccessorTests
    {
        [Fact]
        public void CreateInstance()
        {
            // arrange
            var schemaName = "foo";
            var executor = new Mock<IQueryExecutor>();

            // act
            var accessor = new RemoteExecutorAccessor(
                schemaName, executor.Object);

            // act
            Assert.Equal(schemaName, accessor.SchemaName);
            Assert.Equal(executor.Object, accessor.Executor);
        }

        [Fact]
        public void NameIsNull()
        {
            // arrange
            var executor = new Mock<IQueryExecutor>();

            // act
            Action a = () => new RemoteExecutorAccessor(
                null, executor.Object);

            // act
            Assert.Equal("schemaName",
               Assert.Throws<ArgumentException>(a).ParamName);
        }

        [Fact]
        public void NameIsEmpty()
        {
            // arrange
            var executor = new Mock<IQueryExecutor>();

            // act
            Action a = () => new RemoteExecutorAccessor(
                string.Empty, executor.Object);

            // act
            Assert.Equal("schemaName",
               Assert.Throws<ArgumentException>(a).ParamName);
        }

        [Fact]
        public void ExecutorIsNull()
        {
            // arrange
            var schemaName = "foo";

            // act
            Action a = () => new RemoteExecutorAccessor(schemaName, null);

            // act
            Assert.Equal("executor",
                Assert.Throws<ArgumentNullException>(a).ParamName);
        }
    }
}
