using ChilliCream.Testing;
using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Subscriptions;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class EventMessageArgumentSourceCodeGeneratorTests
    {
        [Fact]
        public void Generate_EventMessage()
        {
            // arrange
            var argumentDescriptor = new ArgumentDescriptor(
                "foo", "bar",
                ArgumentKind.EventMessage,
                typeof(EventMessage));

            var generator = new EventMessageArgumentSourceCodeGenerator();

            // act
            string result = generator.Generate("foo", argumentDescriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void CanHandle_EventMessage_True()
        {
            // arrange
            var argumentDescriptor = new ArgumentDescriptor(
                "foo", "bar",
                ArgumentKind.EventMessage,
                typeof(EventMessage));

            var generator = new EventMessageArgumentSourceCodeGenerator();

            // act
            bool result = generator.CanHandle(argumentDescriptor);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void CanHandle_EventMessage_False()
        {
            // arrange
            var argumentDescriptor = new ArgumentDescriptor(
                "foo", "bar",
                ArgumentKind.Directive,
                typeof(object));

            var generator = new EventMessageArgumentSourceCodeGenerator();

            // act
            bool result = generator.CanHandle(argumentDescriptor);

            // assert
            Assert.False(result);
        }
    }
}
