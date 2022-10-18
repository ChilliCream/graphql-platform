using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Types.Pagination
{
    public class CollectionSegmentTests
    {
        [Fact]
        public async Task CreateCollectionSegment_PageInfoAndItems_PassedCorrectly()
        {
            // arrange
            var pageInfo = new CollectionSegmentInfo(true, true);

            var underlyingCollection = new List<string>();

            var getItems = (CancellationToken _) => new ValueTask<IReadOnlyCollection<object>>(underlyingCollection);

            // act
            var collection = new CollectionSegment(
                getItems,
                pageInfo,
                ct => throw new NotSupportedException());

            
            // assert
            Assert.Equal(pageInfo, collection.Info);
            Assert.Equal(underlyingCollection, await collection.GetItemsAsync(CancellationToken.None));
        }

        [Fact]
        public void CreateCollectionSegment_PageInfoNull_ArgumentNullException()
        {
            // arrange
            var items = new List<string>();

            // act
            Action a = () => new CollectionSegment<string>(
                _ => new ValueTask<IReadOnlyCollection<string>>(items), null, ct => throw new NotSupportedException());

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
            var collection = new CollectionSegment(_ => new ValueTask<IReadOnlyCollection<object>>(items), pageInfo, _ => new ValueTask<int>(2));

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
