using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Integration.InputOutputObjectAreTheSame
{
    public class InputOutputObjectAreTheSame
    {
        [Fact]
        public void CheckIfTypesAreRegisteredCorrectly()
        {
            // arrange
            Schema schema = CreateSchema();

            // act
            bool containsPersonInputType = schema
                .TryGetType("PersonInput", out INamedInputType _);
            bool containsPersonOutputType = schema
                .TryGetType("Person", out INamedOutputType _);

            // assert
            Assert.True(containsPersonInputType);
            Assert.True(containsPersonOutputType);
        }

        [Fact]
        public void ExecuteQueryThatReturnsPerson()
        {
            // arrange
            Schema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(@"{
                person(person: { firstName:""a"", lastName:""b"" }) {
                    lastName
                    firstName
                }
             }");

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        private static Schema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterQueryType<Query>();
                c.RegisterType<ObjectType<Person>>();
                c.RegisterType(new InputObjectType<Person>(
                    d => d.Name("PersonInput")));
            });
        }
    }
}
