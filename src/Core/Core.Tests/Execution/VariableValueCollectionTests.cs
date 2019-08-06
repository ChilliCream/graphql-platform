using System;
using System.Collections.Generic;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Execution
{
    public class VariableValueCollectionTests
    {
        [InlineData(null)]
        [InlineData("")]
        [Theory]
        public void GetVariable_Name_Is_Invalid(string name)
        {
            // arrange
            var values = new Dictionary<string, object>();
            var variables = new VariableValueCollection(
                TypeConversion.Default,
                values);

            // acr
            Action action = () => variables.GetVariable<string>(name);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [InlineData(null)]
        [InlineData("")]
        [Theory]
        public void TryGetVariable_Name_Is_Invalid(string name)
        {
            // arrange
            var values = new Dictionary<string, object>();
            var variables = new VariableValueCollection(
                TypeConversion.Default,
                values);

            // acr
            Action action = () => variables.TryGetVariable<string>(name, out _);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void GetVariable_Casted()
        {
            // arrange
            var values = new Dictionary<string, object>
            {
                { "abc", "def" }
            };

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                values);

            // acr
            string value = variables.GetVariable<string>("abc");

            // assert
            Assert.Equal("def", value);
        }

        [Fact]
        public void GetVariable_Converted()
        {
            // arrange
            var values = new Dictionary<string, object>
            {
                { "abc", "123" }
            };

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                values);

            // acr
            int value = variables.GetVariable<int>("abc");

            // assert
            Assert.Equal(123, value);
        }
    }
}
