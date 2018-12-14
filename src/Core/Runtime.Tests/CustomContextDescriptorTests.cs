using System;
using Xunit;

namespace HotChocolate.Runtime
{
    public class CustomContextDescriptorTests
    {
        [Fact]
        public void CreateCustomContextDescriptor()
        {
            // act
            var descriptor = new CustomContextDescriptor(
                typeof(string), sp => "foo", ExecutionScope.Global);

            // assert
            Assert.Equal(typeof(string), descriptor.Key);
            Assert.Equal(typeof(string), descriptor.Type);
            Assert.Equal("foo", descriptor.Factory(null));
            Assert.Equal(ExecutionScope.Global, descriptor.Scope);
        }

        [Fact]
        public void CreateCustomContextDescriptorFactoryIsNull()
        {
            // act
            var descriptor = new CustomContextDescriptor(
                typeof(string), null, ExecutionScope.Global);

            // assert
            Assert.Equal(typeof(string), descriptor.Key);
            Assert.Equal(typeof(string), descriptor.Type);
            Assert.Null(descriptor.Factory);
            Assert.Equal(ExecutionScope.Global, descriptor.Scope);
        }

        [Fact]
        public void CreateCustomContextDescriptorTypeIsNull()
        {
            // act
            Action action = () => new CustomContextDescriptor(
                null, sp => "foo", ExecutionScope.Global);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
