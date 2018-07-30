using System;
using Xunit;

namespace HotChocolate.Runtime
{
    public class DataLoaderDescriptorTests
    {
        [Fact]
        public void CreateDataLoaderDescriptor()
        {
            // act
            var descriptor = new DataLoaderDescriptor(
                "123", typeof(string), ExecutionScope.Global,
                sp => "foo", (d, c) => null);

            // assert
            Assert.Equal("123", descriptor.Key);
            Assert.Equal(typeof(string), descriptor.Type);
            Assert.Equal(ExecutionScope.Global, descriptor.Scope);
            Assert.Equal("foo", descriptor.Factory(null));
            Assert.NotNull(descriptor.TriggerLoadAsync);
        }

        [Fact]
        public void CreateDataLoaderDescriptorFactoryIsNull()
        {
            // act
            var descriptor = new DataLoaderDescriptor(
                "123", typeof(string), ExecutionScope.Global,
                null, (d, c) => null);

            // assert
            Assert.Equal("123", descriptor.Key);
            Assert.Equal(typeof(string), descriptor.Type);
            Assert.Equal(ExecutionScope.Global, descriptor.Scope);
            Assert.Null(descriptor.Factory);
            Assert.NotNull(descriptor.TriggerLoadAsync);
        }

        [Fact]
        public void CreateDataLoaderDescriptorTriggerIsNull()
        {
            // act
            var descriptor = new DataLoaderDescriptor(
                "123", typeof(string), ExecutionScope.Global,
                sp => "foo", null);

            // assert
            Assert.Equal("123", descriptor.Key);
            Assert.Equal(typeof(string), descriptor.Type);
            Assert.Equal(ExecutionScope.Global, descriptor.Scope);
            Assert.Equal("foo", descriptor.Factory(null));
            Assert.Null(descriptor.TriggerLoadAsync);
        }

        [Fact]
        public void CreateDataLoaderDescriptorKeyIsNull()
        {
            // act
            Action action = () => new DataLoaderDescriptor(
                null, typeof(string), ExecutionScope.Global,
                sp => "foo", null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void CreateDataLoaderDescriptorTypeIsNull()
        {
            // act
            Action action = () => new DataLoaderDescriptor(
                "123", null, ExecutionScope.Global,
                sp => "foo", null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

    }
}
