using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Types.Pagination
{
    public class CollectionSegmentTests
    {
        [Fact]
        public void CreateCollectionSegment_PageInfoAndItems_PassedCorrectly()
        {
            // arrange
            var pageInfo = new CollectionSegmentInfo(true, true);
            var items = new List<string>();

            // act
            var collection = new CollectionSegment(
                items,
                pageInfo,
                ct => throw new NotSupportedException());

            // assert
            Assert.Equal(pageInfo, collection.Info);
            Assert.Equal(items, collection.Items);
        }

        [Fact]
        public void CreateCollectionSegment_PageInfoNull_ArgumentNullException()
        {
            // arrange
            var items = new List<string>();

            // act
            Action a = () => new CollectionSegment<string>(
                items, null, ct => throw new NotSupportedException());

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void CreateCollectionSegment_ItemsNull_ArgumentNullException()
        {
            // arrange
            var pageInfo = new CollectionSegmentInfo(true, true);

            // act
            void Verify() => new CollectionSegment<string>(
                null!,
                pageInfo,
                _ => throw new NotSupportedException());

            // assert
            Assert.Throws<ArgumentNullException>(Verify);
        }

        [Fact]
        public async Task GetTotalCountAsync_Delegate_ReturnsTotalCount()
        {
            // arrange
            var pageInfo = new CollectionSegmentInfo(true, true);
            var items = new List<string>();

            // act
            var collection = new CollectionSegment(items, pageInfo, _ => new ValueTask<int>(2));

            // assert
            Assert.Equal(2, await collection.GetTotalCountAsync(default));
        }

        [Fact]
        public async Task GetTotalCountAsync_Value_ReturnsTotalCount()
        {
            // arrange
            var pageInfo = new CollectionSegmentInfo(true, true);
            var items = new List<string>();

            // act
            var collection = new CollectionSegment(items, pageInfo, 2);

            // assert
            Assert.Equal(2, await collection.GetTotalCountAsync(default));
        }
    }
}
