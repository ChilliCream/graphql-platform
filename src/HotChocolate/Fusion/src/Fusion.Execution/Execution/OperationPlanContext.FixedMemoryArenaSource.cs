using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Execution;

public sealed partial class OperationPlanContext
{
    private sealed class FixedMemoryArenaSource : IMemoryArenaSource
    {
        private IMemoryArena? _arena;

        public IMemoryArena GetNextArena()
            => _arena ?? throw new InvalidOperationException(
                "The arena source has not been initialized.");

        public void Set(MemoryArena arena)
            => _arena = arena;

        public void Clear()
            => _arena = null;
    }
}
