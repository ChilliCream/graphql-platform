using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.ApolloFederation
{
    public class FieldSetTypeTests
    {
        [Fact]
        public void Deserialize()
        {
            // arrange
            var type = new FieldSetType();
            const string serialized = "a b c d e(d: $b)";

            // act
            object selectionSet = type.Deserialize(serialized);

            // assert
            Assert.IsType<SelectionSetNode>(selectionSet);
        }

        [Fact]
        public void Deserialize_Invalid_Format()
        {
            // arrange
            var type = new FieldSetType();
            const string serialized = "1";

            // act
            void Action() => type.Deserialize(serialized);

            // assert
            Assert.Throws<SerializationException>(Action);
        }
    }
}
