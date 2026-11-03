using HotChocolate.Buffers;

namespace HotChocolate.Text.Json;

public class ResultDocumentTests
{
    // Collecting a document whose constructor threw must not end the process.
    [Fact]
    public void Constructor_Should_LeaveCollectableObject_When_ArenaRentThrows()
    {
        // arrange
        var operation = CommonTestExtensions.CreateOperation();
        var arena = new ThrowingMemoryArena();

        // act
        var exception = Record.Exception(() => new ResultDocument(arena, operation, 0));
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // assert
        Assert.IsType<OutOfMemoryException>(exception);
    }

    private sealed class ThrowingMemoryArena : IMemoryArena
    {
        public MemorySegment Rent(int size) => throw new OutOfMemoryException();

        public MemorySegment[] RentSegmentTable(int minLength) => throw new OutOfMemoryException();

        public void GrowSegmentTable(ref MemorySegment[] table) => throw new OutOfMemoryException();
    }
}
