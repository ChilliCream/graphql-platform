using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Utilities
{
    public class StackableListTest
    {
        [Fact]
        public void Push_Should_AddElementsAtTheEnd()
        {
            // arrange
            var stack = new StackableList<int>();

            // act 
            stack.Add(10);
            stack.Add(11);

            // assert
            List<int>.Enumerator enumerator = stack.GetEnumerator();
            enumerator.MoveNext();
            Assert.Equal(10, enumerator.Current);
            enumerator.MoveNext();
            Assert.Equal(11, enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void Pop_Should_RemoveItemsFromTheEnd()
        {
            // arrange
            var stack = new StackableList<int>();

            // act 
            stack.Add(10);
            stack.Add(11);
            stack.Pop();

            // assert
            List<int>.Enumerator enumerator = stack.GetEnumerator();
            enumerator.MoveNext();
            Assert.Equal(10, enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void Pop_Should_ReturnRemoveElement()
        {
            // arrange
            var stack = new StackableList<int>();

            // act 
            stack.Add(10);
            stack.Add(11);
            var removedEelement = stack.Pop();

            // assert
            Assert.Equal(11, removedEelement);
        }

        [Fact]
        public void Peek_Should_ReturnLastElement()
        {
            // arrange
            var stack = new StackableList<int>();

            // act 
            stack.Add(10);
            stack.Add(11);
            var peekedElement = stack.Peek();

            // assert
            Assert.Equal(11, peekedElement);
        }

        [Fact]
        public void TryPeek_Should_ReturnLastElement()
        {
            // arrange
            var stack = new StackableList<int>();

            // act 
            stack.Add(10);
            stack.Add(11);
            var result = stack.TryPeek(out var peekedElement);

            // assert
            Assert.True(result);
            Assert.Equal(11, peekedElement);
        }

        [Fact]
        public void PeekAt_Should_ReturnElementAtPosition()
        {
            // arrange
            var stack = new StackableList<int>();

            // act 
            stack.Add(10);
            stack.Add(11);
            var peekedElement = stack.PeekAt(1);

            // assert
            Assert.Equal(10, peekedElement);
        }

        [Fact]
        public void TryPeekAt_Should_ReturnElementAtPosition()
        {
            // arrange
            var stack = new StackableList<int>();

            // act 
            stack.Add(10);
            stack.Add(11);
            var result = stack.TryPeekAt(1, out var peekedElement);

            // assert
            Assert.Equal(10, peekedElement);
            Assert.True(result);
        }

        [Fact]
        public void TryPeekAt_Should_NotFailIfIndexToBig()
        {
            // arrange
            var stack = new StackableList<int>();

            // act 
            stack.Add(10);
            stack.Add(11);
            var result = stack.TryPeekAt(3, out var peekedElement);

            // assert
            Assert.Equal(0, peekedElement);
            Assert.False(result);
        }

        [Fact]
        public void TryPeek_Should_NotFailOnEmptyList()
        {
            // arrange
            var stack = new StackableList<int>();

            // act 
            var result = stack.TryPeek(out var peekedElement);

            // assert
            Assert.Equal(0, peekedElement);
            Assert.False(result);
        }
    }
}
