using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;

namespace Fusion.Execution.Benchmarks;

[MemoryDiagnoser]
[InProcess]
public class HashCodeBenchmark
{
    private byte[] _data = null!;

    [Params(8, 16, 24, 32, 48, 64, 96, 128, 256, 512)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[Size];
        var rng = new Random(42);
        rng.NextBytes(_data);
    }

    [Benchmark(Baseline = true)]
    public int ScalarLoop()
    {
        return (int)(ComputeHashScalar(0u, _data) & 0x7FFFFFFF);
    }

    [Benchmark]
    public int SimdHash()
    {
        return (int)(ComputeHashSimd(0u, _data) & 0x7FFFFFFF);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ComputeHashScalar(uint hash, ReadOnlySpan<byte> bytes)
    {
        unchecked
        {
            foreach (var b in bytes)
            {
                hash = (hash * 31) + b;
            }
        }

        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ComputeHashSimd(uint hash, ReadOnlySpan<byte> bytes)
    {
        unchecked
        {
            const uint pow31_1 = 31;
            const uint pow31_2 = 31 * 31;
            const uint pow31_3 = 31 * 31 * 31;
            const uint pow31_4 = 31 * 31 * 31 * 31;
            const uint pow31_5 = pow31_4 * 31;
            const uint pow31_6 = pow31_5 * 31;
            const uint pow31_7 = pow31_6 * 31;
            const uint pow31_8 = pow31_7 * 31;

            ref var src = ref MemoryMarshal.GetReference(bytes);
            var i = 0;

            if (Vector256.IsHardwareAccelerated && bytes.Length >= 64)
            {
                var acc = Vector256<uint>.Zero;
                var mul = Vector256.Create(pow31_8);
                var simdEnd = bytes.Length & ~7;

                for (; i < simdEnd; i += 8)
                {
                    var raw = Vector128.CreateScalarUnsafe(
                        Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref src, i))).AsByte();
                    var (loShort, _) = Vector128.Widen(raw);
                    var (lo32, hi32) = Vector128.Widen(loShort);
                    var wide = Vector256.Create(lo32, hi32);

                    acc = (acc * mul) + wide;
                }

                var finalPow = Vector256.Create(
                    pow31_7, pow31_6, pow31_5, pow31_4,
                    pow31_3, pow31_2, pow31_1, 1u);
                acc *= finalPow;

                var sum128 = acc.GetLower() + acc.GetUpper();
                var t = sum128 + Vector128.Shuffle(sum128, Vector128.Create(2u, 3u, 0u, 1u));
                var simdResult = (t + Vector128.Shuffle(t, Vector128.Create(1u, 0u, 3u, 2u))).ToScalar();

                hash = (hash * Pow31(simdEnd)) + simdResult;
            }

            if (Vector128.IsHardwareAccelerated && bytes.Length - i >= 4)
            {
                var acc = Vector128<uint>.Zero;
                var mul = Vector128.Create(pow31_4);
                var simdEnd = i + ((bytes.Length - i) & ~3);
                var simdStart = i;

                for (; i < simdEnd; i += 4)
                {
                    var raw = Vector128.CreateScalarUnsafe(
                        Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref src, i))).AsByte();
                    var (loShort, _) = Vector128.Widen(raw);
                    var (wide, _) = Vector128.Widen(loShort);

                    acc = (acc * mul) + wide;
                }

                var finalPow = Vector128.Create(pow31_3, pow31_2, pow31_1, 1u);
                acc *= finalPow;

                var t = acc + Vector128.Shuffle(acc, Vector128.Create(2u, 3u, 0u, 1u));
                var simdResult = (t + Vector128.Shuffle(t, Vector128.Create(1u, 0u, 3u, 2u))).ToScalar();

                hash = (hash * Pow31(simdEnd - simdStart)) + simdResult;
            }

            // Scalar tail for remaining bytes.
            for (; i < bytes.Length; i++)
            {
                hash = (hash * 31) + Unsafe.Add(ref src, i);
            }

            return hash;
        }
    }

    private static uint Pow31(int n)
    {
        unchecked
        {
            var result = 1u;
            var b = 31u;

            while (n > 0)
            {
                if ((n & 1) != 0)
                {
                    result *= b;
                }

                b *= b;
                n >>= 1;
            }

            return result;
        }
    }
}
