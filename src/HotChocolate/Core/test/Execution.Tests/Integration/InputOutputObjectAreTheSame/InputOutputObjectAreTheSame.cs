﻿using System.Threading.Tasks;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Integration.InputOutputObjectAreTheSame
{
    public class InputOutputObjectAreTheSame
    {
        [Fact]
        public void CheckIfTypesAreRegisteredCorrectly()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            var containsPersonInputType = schema.TryGetType("PersonInput", out INamedInputType _);
            var containsPersonOutputType = schema.TryGetType("Person", out INamedOutputType _);

            // assert
            Assert.True(containsPersonInputType);
            Assert.True(containsPersonOutputType);
        }

        [Fact]
        public async Task ExecuteQueryThatReturnsPerson()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(@"{
                    person(person: { firstName:""a"", lastName:""b"" }) {
                        lastName
                        firstName
                    }
                }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        private static ISchema CreateSchema()
            => SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddType<ObjectType<Person>>()
                .AddType(new InputObjectType<Person>(
                    d => d.Name("PersonInput")))
                .Create();
    }
}
