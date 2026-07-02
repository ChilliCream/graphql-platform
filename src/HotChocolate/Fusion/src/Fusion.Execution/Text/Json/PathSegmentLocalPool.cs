using System.Buffers;

namespace HotChocolate.Fusion.Text.Json;

internal sealed class PathSegmentLocalPool : IDisposable
{
    private int[]?[] _buffers;
    private int _index;
    private int[]?[] _allRented;
    private int _allRentedCount;
    private bool _disposed;

    public PathSegmentLocalPool(int initialCapacity = 64)
    {
        var capacity = Math.Max(32, initialCapacity);

        _buffers = ArrayPool<int[]?>.Shared.Rent(capacity);
        _index = 0;
        _allRented = ArrayPool<int[]?>.Shared.Rent(capacity * 2);
        _allRentedCount = 0;
    }

    public int[] Rent()
    {
        if (_index > 0)
        {
            var array = _buffers[--_index]!;
            _buffers[_index] = null;
            return array;
        }

        var rented = PathSegmentMemory.Rent();
        TrackRented(rented);
        return rented;
    }

    public void Return(int[] array)
    {
        if (array.Length != PathSegmentMemory.SegmentArraySize)
        {
            return;
        }

        if (_index == _buffers.Length)
        {
            GrowBuffers();
        }

        _buffers[_index++] = array;
    }

    private void TrackRented(int[] array)
    {
        if (_allRentedCount == _allRented.Length)
        {
            GrowAllRented();
        }

        _allRented[_allRentedCount++] = array;
    }

    private void GrowBuffers()
    {
        var newBuffers = ArrayPool<int[]?>.Shared.Rent(_buffers.Length * 2);
        _buffers.AsSpan(0, _index).CopyTo(newBuffers);
        ArrayPool<int[]?>.Shared.Return(_buffers, clearArray: true);
        _buffers = newBuffers;
    }

    private void GrowAllRented()
    {
        var newAllRented = ArrayPool<int[]?>.Shared.Rent(_allRented.Length * 2);
        _allRented.AsSpan(0, _allRentedCount).CopyTo(newAllRented);
        ArrayPool<int[]?>.Shared.Return(_allRented, clearArray: true);
        _allRented = newAllRented;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        for (var i = 0; i < _allRentedCount; i++)
        {
            PathSegmentMemory.Return(_allRented[i]!);
            _allRented[i] = null;
        }

        _allRentedCount = 0;
        _index = 0;

        ArrayPool<int[]?>.Shared.Return(_buffers, clearArray: true);
        ArrayPool<int[]?>.Shared.Return(_allRented, clearArray: true);

        _buffers = [];
        _allRented = [];
    }
}
