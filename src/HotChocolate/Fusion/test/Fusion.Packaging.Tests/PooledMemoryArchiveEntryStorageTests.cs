using System.Buffers;
using HotChocolate.Fusion.Packaging.Storage;

namespace HotChocolate.Fusion.Packaging;

public sealed class PooledMemoryArchiveEntryStorageTests
{
    [Fact]
    public void Dispose_Should_ClearRentedBufferBeforeReturningIt()
    {
        var pool = new RecordingArrayPool();
        var storage = new PooledMemoryArchiveEntryStorage(pool);
        using (var stream = storage.OpenWrite(16))
        {
            stream.Write("sensitive"u8);
        }

        storage.Dispose();

        Assert.True(pool.ClearArray);
        Assert.NotNull(pool.ReturnedArray);
        Assert.All(pool.ReturnedArray, value => Assert.Equal(0, value));
    }

    private sealed class RecordingArrayPool : ArrayPool<byte>
    {
        public byte[]? ReturnedArray { get; private set; }

        public bool ClearArray { get; private set; }

        public override byte[] Rent(int minimumLength)
            => new byte[minimumLength];

        public override void Return(byte[] array, bool clearArray = false)
        {
            if (clearArray)
            {
                array.AsSpan().Clear();
            }

            ReturnedArray = array;
            ClearArray = clearArray;
        }
    }
}
