using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;

namespace Fusion.Execution.Benchmarks;

/// <summary>
/// Compares row-write strategies for each MetaDb append pattern.
/// All benchmarks write <see cref="Rows"/> rows of 20 bytes each to a
/// pre-allocated buffer; we measure the per-row write cost only.
///
/// Patterns (mapped to real MetaDb methods):
///   - AppendNull:            1 variable int  + 4 zero ints
///   - AppendEmptyProperty:   2 variable ints + 3 zero ints
///   - AppendStartObject:     4 variable ints + 1 zero int
/// </summary>
internal static class DbRowBenchData
{
    public const int RowSize = 20;
    public const int Rows = 4096;

    public static byte[] Buffer { get; } = new byte[Rows * RowSize];
    public static int[] Parents { get; } = new int[Rows];
    public static int[] SelectionIds { get; } = new int[Rows];
    public static int[] PropertyCounts { get; } = new int[Rows];
    public static int[] Flags { get; } = new int[Rows];

    static DbRowBenchData()
    {
        var rng = new Random(42);
        for (var i = 0; i < Rows; i++)
        {
            Parents[i] = rng.Next(0, 0x0FFFFFFF);
            SelectionIds[i] = rng.Next(0, 0x7FFF);
            PropertyCounts[i] = rng.Next(0, 32);
            Flags[i] = rng.Next(0, 63);
        }
    }

    // ---------------- AppendNull ----------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteNull_FiveScalar(ref byte row, int parent)
    {
        Unsafe.WriteUnaligned(ref row, parent << 4);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 4), 0);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 8), 0);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 12), 0);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 16), 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteNull_ScalarPlusInitBlock(ref byte row, int parent)
    {
        Unsafe.WriteUnaligned(ref row, parent << 4);
        Unsafe.InitBlockUnaligned(ref Unsafe.Add(ref row, 4), 0, 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteNull_Vec128PlusScalar(ref byte row, int parent)
    {
        // 16-byte zero-ish vector with int0 set, then trailing int zero.
        var v = Vector128.Create(parent << 4, 0, 0, 0).AsByte();
        v.StoreUnsafe(ref row);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 16), 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteNull_ScalarPlusVec128Zero(ref byte row, int parent)
    {
        // Scalar write for int0, then a 16-byte Vector128 zero covering ints 1..4.
        Unsafe.WriteUnaligned(ref row, parent << 4);
        Vector128<byte>.Zero.StoreUnsafe(ref Unsafe.Add(ref row, 4));
    }

    // ---------------- AppendEmptyProperty ----------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteEmptyProp_FiveScalar(ref byte row, int parent, int selectionId, int flags)
    {
        Unsafe.WriteUnaligned(ref row, 3 /*PropertyName*/ | (parent << 4));
        Unsafe.WriteUnaligned(
            ref Unsafe.Add(ref row, 4),
            selectionId | (2 << 15) | (flags << 17));
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 8), 0);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 12), 0);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 16), 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteEmptyProp_TwoScalarPlusInitBlock(ref byte row, int parent, int selectionId, int flags)
    {
        Unsafe.WriteUnaligned(ref row, 3 | (parent << 4));
        Unsafe.WriteUnaligned(
            ref Unsafe.Add(ref row, 4),
            selectionId | (2 << 15) | (flags << 17));
        Unsafe.InitBlockUnaligned(ref Unsafe.Add(ref row, 8), 0, 12);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteEmptyProp_Vec128PlusScalar(ref byte row, int parent, int selectionId, int flags)
    {
        var int0 = 3 | (parent << 4);
        var int1 = selectionId | (2 << 15) | (flags << 17);

        var v = Vector128.Create(int0, int1, 0, 0).AsByte();
        v.StoreUnsafe(ref row);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 16), 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteEmptyProp_TwoScalarPlusVec128Zero(ref byte row, int parent, int selectionId, int flags)
    {
        Unsafe.WriteUnaligned(ref row, 3 | (parent << 4));
        Unsafe.WriteUnaligned(
            ref Unsafe.Add(ref row, 4),
            selectionId | (2 << 15) | (flags << 17));
        // Covers ints 2..4 using a 16-byte zero store (overwrites 4 bytes past end, but row is 20B
        // and buffer is row-aligned multiples of 20B — works only when room exists after).
        // To be safe here we do two stores: Vec128.Zero at offset 8 would write 16 bytes,
        // but only 12 are in our row. Fallback: 3 scalar zeros.
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 8), 0);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 12), 0);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 16), 0);
    }

    // ---------------- AppendStartObject ----------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObj_FiveScalar(ref byte row, int parent, int selectionId, int propertyCount, int flags)
    {
        Unsafe.WriteUnaligned(ref row, 1 | (parent << 4));
        Unsafe.WriteUnaligned(
            ref Unsafe.Add(ref row, 4),
            selectionId | (1 << 15) | (flags << 17));
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 8), propertyCount);
        Unsafe.WriteUnaligned(
            ref Unsafe.Add(ref row, 12),
            ((propertyCount * 2) + 1) & 0x07FFFFFF);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 16), 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObj_Struct(ref byte row, int parent, int selectionId, int propertyCount, int flags)
    {
        var dbRow = new DbRowLocal(
            tokenType: 1,
            sizeOrLength: propertyCount,
            parentRow: parent,
            operationReferenceId: selectionId,
            operationReferenceType: 1,
            numberOfRows: (propertyCount * 2) + 1,
            flags: flags);
        Unsafe.WriteUnaligned(ref row, dbRow);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObj_Vec128PlusScalar(ref byte row, int parent, int selectionId, int propertyCount, int flags)
    {
        var int0 = 1 | (parent << 4);
        var int1 = selectionId | (1 << 15) | (flags << 17);
        var int2 = propertyCount;
        var int3 = ((propertyCount * 2) + 1) & 0x07FFFFFF;

        var v = Vector128.Create(int0, int1, int2, int3).AsByte();
        v.StoreUnsafe(ref row);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 16), 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct DbRowLocal
    {
        private readonly int _typeAndParent;
        private readonly int _selectionAndFlags;
        private readonly int _sizeOrLengthUnion;
        private readonly int _locationOrRows;
        private readonly int _source;

        public DbRowLocal(
            int tokenType,
            int location = 0,
            int sizeOrLength = 0,
            int sourceDocumentId = 0,
            int parentRow = 0,
            int operationReferenceId = 0,
            int operationReferenceType = 0,
            int numberOfRows = 0,
            int flags = 0)
        {
            var locationOrRows = location != 0 ? location : numberOfRows;
            _typeAndParent = (tokenType & 0x0F) | (parentRow << 4);
            _selectionAndFlags = operationReferenceId
                | (operationReferenceType << 15)
                | (flags << 17);
            _sizeOrLengthUnion = sizeOrLength;
            _locationOrRows = locationOrRows & 0x07FFFFFF;
            _source = sourceDocumentId & 0x7FFF;
        }
    }
}

[MemoryDiagnoser]
[InProcess]
public class AppendNullWriteBenchmark
{
    [Benchmark(Baseline = true)]
    public void FiveScalar()
    {
        ref var dest = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Buffer);
        ref var parents = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Parents);

        for (var i = 0; i < DbRowBenchData.Rows; i++)
        {
            DbRowBenchData.WriteNull_FiveScalar(
                ref Unsafe.Add(ref dest, i * DbRowBenchData.RowSize),
                Unsafe.Add(ref parents, i));
        }
    }

    [Benchmark]
    public void ScalarPlusInitBlock()
    {
        ref var dest = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Buffer);
        ref var parents = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Parents);

        for (var i = 0; i < DbRowBenchData.Rows; i++)
        {
            DbRowBenchData.WriteNull_ScalarPlusInitBlock(
                ref Unsafe.Add(ref dest, i * DbRowBenchData.RowSize),
                Unsafe.Add(ref parents, i));
        }
    }

    [Benchmark]
    public void Vec128PlusScalar()
    {
        ref var dest = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Buffer);
        ref var parents = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Parents);

        for (var i = 0; i < DbRowBenchData.Rows; i++)
        {
            DbRowBenchData.WriteNull_Vec128PlusScalar(
                ref Unsafe.Add(ref dest, i * DbRowBenchData.RowSize),
                Unsafe.Add(ref parents, i));
        }
    }

    [Benchmark]
    public void ScalarPlusVec128Zero()
    {
        ref var dest = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Buffer);
        ref var parents = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Parents);

        for (var i = 0; i < DbRowBenchData.Rows; i++)
        {
            DbRowBenchData.WriteNull_ScalarPlusVec128Zero(
                ref Unsafe.Add(ref dest, i * DbRowBenchData.RowSize),
                Unsafe.Add(ref parents, i));
        }
    }
}

[MemoryDiagnoser]
[InProcess]
public class AppendEmptyPropertyWriteBenchmark
{
    [Benchmark(Baseline = true)]
    public void FiveScalar()
    {
        ref var dest = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Buffer);
        ref var parents = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Parents);
        ref var selIds = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.SelectionIds);
        ref var flags = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Flags);

        for (var i = 0; i < DbRowBenchData.Rows; i++)
        {
            DbRowBenchData.WriteEmptyProp_FiveScalar(
                ref Unsafe.Add(ref dest, i * DbRowBenchData.RowSize),
                Unsafe.Add(ref parents, i),
                Unsafe.Add(ref selIds, i),
                Unsafe.Add(ref flags, i));
        }
    }

    [Benchmark]
    public void TwoScalarPlusInitBlock()
    {
        ref var dest = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Buffer);
        ref var parents = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Parents);
        ref var selIds = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.SelectionIds);
        ref var flags = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Flags);

        for (var i = 0; i < DbRowBenchData.Rows; i++)
        {
            DbRowBenchData.WriteEmptyProp_TwoScalarPlusInitBlock(
                ref Unsafe.Add(ref dest, i * DbRowBenchData.RowSize),
                Unsafe.Add(ref parents, i),
                Unsafe.Add(ref selIds, i),
                Unsafe.Add(ref flags, i));
        }
    }

    [Benchmark]
    public void Vec128PlusScalar()
    {
        ref var dest = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Buffer);
        ref var parents = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Parents);
        ref var selIds = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.SelectionIds);
        ref var flags = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Flags);

        for (var i = 0; i < DbRowBenchData.Rows; i++)
        {
            DbRowBenchData.WriteEmptyProp_Vec128PlusScalar(
                ref Unsafe.Add(ref dest, i * DbRowBenchData.RowSize),
                Unsafe.Add(ref parents, i),
                Unsafe.Add(ref selIds, i),
                Unsafe.Add(ref flags, i));
        }
    }
}

[MemoryDiagnoser]
[InProcess]
public class AppendStartObjectWriteBenchmark
{
    [Benchmark(Baseline = true)]
    public void FiveScalar()
    {
        ref var dest = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Buffer);
        ref var parents = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Parents);
        ref var selIds = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.SelectionIds);
        ref var counts = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.PropertyCounts);
        ref var flags = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Flags);

        for (var i = 0; i < DbRowBenchData.Rows; i++)
        {
            DbRowBenchData.WriteStartObj_FiveScalar(
                ref Unsafe.Add(ref dest, i * DbRowBenchData.RowSize),
                Unsafe.Add(ref parents, i),
                Unsafe.Add(ref selIds, i),
                Unsafe.Add(ref counts, i),
                Unsafe.Add(ref flags, i));
        }
    }

    [Benchmark]
    public void DbRowStruct()
    {
        ref var dest = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Buffer);
        ref var parents = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Parents);
        ref var selIds = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.SelectionIds);
        ref var counts = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.PropertyCounts);
        ref var flags = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Flags);

        for (var i = 0; i < DbRowBenchData.Rows; i++)
        {
            DbRowBenchData.WriteStartObj_Struct(
                ref Unsafe.Add(ref dest, i * DbRowBenchData.RowSize),
                Unsafe.Add(ref parents, i),
                Unsafe.Add(ref selIds, i),
                Unsafe.Add(ref counts, i),
                Unsafe.Add(ref flags, i));
        }
    }

    [Benchmark]
    public void Vec128PlusScalar()
    {
        ref var dest = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Buffer);
        ref var parents = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Parents);
        ref var selIds = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.SelectionIds);
        ref var counts = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.PropertyCounts);
        ref var flags = ref MemoryMarshal.GetArrayDataReference(DbRowBenchData.Flags);

        for (var i = 0; i < DbRowBenchData.Rows; i++)
        {
            DbRowBenchData.WriteStartObj_Vec128PlusScalar(
                ref Unsafe.Add(ref dest, i * DbRowBenchData.RowSize),
                Unsafe.Add(ref parents, i),
                Unsafe.Add(ref selIds, i),
                Unsafe.Add(ref counts, i),
                Unsafe.Add(ref flags, i));
        }
    }
}
