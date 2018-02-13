
using System;
using Zeus.Abstractions;
using Xunit;

namespace Zeus.Execution
{
    public class SelectionResultProcessorResolverTests
    {
        [Fact]
        public void ScalarListType()
        {
            // arrange
            ListType type = new ListType(NamedType.String);

            // act
            ISelectionResultProcessor processor = SelectionResultProcessorResolver.GetProcessor(type);

            // assert
            Assert.Equal(ScalarListSelectionResultProcessor.Default, processor);
        }

        [Fact]
        public void ObjectListType()
        {
            // arrange
            ListType type = new ListType(new NamedType("MyObject"));

            // act
            ISelectionResultProcessor processor = SelectionResultProcessorResolver.GetProcessor(type);

            // assert
            Assert.Equal(ObjectListSelectionResultProcessor.Default, processor);
        }

        [Fact]
        public void ScalarType()
        {
            // arrange
            NamedType type = NamedType.Boolean;

            // act
            ISelectionResultProcessor processor = SelectionResultProcessorResolver.GetProcessor(type);

            // assert
            Assert.Equal(ScalarSelectionResultProcessor.Default, processor);
        }

        [Fact]
        public void ObjectType()
        {
            // arrange
            NamedType type = new NamedType("MyObject");

            // act
            ISelectionResultProcessor processor = SelectionResultProcessorResolver.GetProcessor(type);

            // assert
            Assert.Equal(ObjectSelectionResultProcessor.Default, processor);
        }

        [Fact]
        public void TypeIsNull()
        {
            // act
            Action a = () => SelectionResultProcessorResolver.GetProcessor(null);

            // assert
            Assert.Throws<ArgumentNullException>("fieldType", a);
        }
    }
}