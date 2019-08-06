using System;
using System.Collections.Generic;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
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

            // act
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

            // act
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

            // act
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

            // act
            int value = variables.GetVariable<int>("abc");

            // assert
            Assert.Equal(123, value);
        }


        [Fact]
        public void GetVariable_Variable_Does_Not_Exist()
        {
            // arrange
            var values = new Dictionary<string, object>
            {
                { "abc", "123" }
            };

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                values);

            // act
            Action action = () => variables.GetVariable<int>("def");

            // assert
            Assert.Throws<QueryException>(action)
                .Errors.MatchSnapshot();
        }

        [Fact]
        public void TryGetVariable_Casted()
        {
            // arrange
            var values = new Dictionary<string, object>
            {
                { "abc", "def" }
            };

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                values);

            // act
            bool success = variables.TryGetVariable<string>(
                "abc", out var value);

            // assert
            Assert.True(success);
            Assert.Equal("def", value);
        }

        [Fact]
        public void TryGetVariable_Converted()
        {
            // arrange
            var values = new Dictionary<string, object>
            {
                { "abc", "123" }
            };

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                values);

            // act
            bool success = variables.TryGetVariable<int>(
                "abc", out var value);

            // assert
            Assert.True(success);
            Assert.Equal(123, value);
        }

        [Fact]
        public void TryGetVariable_Variable_Does_Not_Exist()
        {
            // arrange
            var values = new Dictionary<string, object>
            {
                { "abc", "123" }
            };

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                values);

            // act
            bool success = variables.TryGetVariable<int>(
                "def", out var value);

            // assert
            Assert.False(success);
            Assert.Equal(default(int), value);
        }

        [Fact]
        public void CreateInstance_ValuesIsNull()
        {
            // arrange
            // act
            Action action = () => new VariableValueCollection(
                TypeConversion.Default,
                null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void CreateInstance_ConverterIsNull()
        {
            // arrange
            var values = new Dictionary<string, object>
            {
                { "abc", "123" }
            };

            // act
            Action action = () => new VariableValueCollection(
                null,
                values);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
