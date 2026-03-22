using System.Collections.Concurrent;
using System.Collections.Frozen;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mocha.Mediator.Benchmarks.Internal;

/// <summary>
/// Compares different dispatch strategies for routing messages to handlers.
/// Tests switch-based pattern matching vs dictionary lookup approaches.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class DispatchPatternBenchmarks
{
    // 20 dummy message types
    public sealed record Msg00(Guid Id);
    public sealed record Msg01(Guid Id);
    public sealed record Msg02(Guid Id);
    public sealed record Msg03(Guid Id);
    public sealed record Msg04(Guid Id);
    public sealed record Msg05(Guid Id);
    public sealed record Msg06(Guid Id);
    public sealed record Msg07(Guid Id);
    public sealed record Msg08(Guid Id);
    public sealed record Msg09(Guid Id);
    public sealed record Msg10(Guid Id);
    public sealed record Msg11(Guid Id);
    public sealed record Msg12(Guid Id);
    public sealed record Msg13(Guid Id);
    public sealed record Msg14(Guid Id);
    public sealed record Msg15(Guid Id);
    public sealed record Msg16(Guid Id);
    public sealed record Msg17(Guid Id);
    public sealed record Msg18(Guid Id);
    public sealed record Msg19(Guid Id);

    private static readonly Guid _result = Guid.NewGuid();
    private static readonly Func<object, Guid> _handler = _ => _result;

    private object[] _messages = null!;
    private object _targetMessage = null!;

    private Dictionary<Type, Func<object, Guid>> _dictionary = null!;
    private FrozenDictionary<Type, Func<object, Guid>> _frozenDictionary = null!;
    private ConcurrentDictionary<Type, Func<object, Guid>> _concurrentDictionary = null!;

    [StructLayout(LayoutKind.Sequential)]
    private struct Entry
    {
        public nint TypeHandle;
        public Func<object, Guid> Handler;
    }

    private Entry[] _table = null!;
    private int _mask;
    private int _shift;
    private ulong _multiplier;

    private FrozenDictionary<nint, Func<object, Guid>> _handleFrozenDictionary = null!;

    public enum MessagePosition
    {
        First,
        Middle,
        Last
    }

    [Params(MessagePosition.First, MessagePosition.Middle, MessagePosition.Last)]
    public MessagePosition Position { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _messages =
        [
            new Msg00(Guid.NewGuid()), new Msg01(Guid.NewGuid()), new Msg02(Guid.NewGuid()),
            new Msg03(Guid.NewGuid()), new Msg04(Guid.NewGuid()), new Msg05(Guid.NewGuid()),
            new Msg06(Guid.NewGuid()), new Msg07(Guid.NewGuid()), new Msg08(Guid.NewGuid()),
            new Msg09(Guid.NewGuid()), new Msg10(Guid.NewGuid()), new Msg11(Guid.NewGuid()),
            new Msg12(Guid.NewGuid()), new Msg13(Guid.NewGuid()), new Msg14(Guid.NewGuid()),
            new Msg15(Guid.NewGuid()), new Msg16(Guid.NewGuid()), new Msg17(Guid.NewGuid()),
            new Msg18(Guid.NewGuid()), new Msg19(Guid.NewGuid())
        ];

        _targetMessage = Position switch
        {
            MessagePosition.First => _messages[0],
            MessagePosition.Middle => _messages[10],
            MessagePosition.Last => _messages[19],
            _ => throw new ArgumentOutOfRangeException()
        };

        var entries = new (Type, Func<object, Guid>)[]
        {
            (typeof(Msg00), _handler), (typeof(Msg01), _handler), (typeof(Msg02), _handler),
            (typeof(Msg03), _handler), (typeof(Msg04), _handler), (typeof(Msg05), _handler),
            (typeof(Msg06), _handler), (typeof(Msg07), _handler), (typeof(Msg08), _handler),
            (typeof(Msg09), _handler), (typeof(Msg10), _handler), (typeof(Msg11), _handler),
            (typeof(Msg12), _handler), (typeof(Msg13), _handler), (typeof(Msg14), _handler),
            (typeof(Msg15), _handler), (typeof(Msg16), _handler), (typeof(Msg17), _handler),
            (typeof(Msg18), _handler), (typeof(Msg19), _handler)
        };

        _dictionary = new Dictionary<Type, Func<object, Guid>>(entries.Length);
        foreach (var (type, handler) in entries)
        {
            _dictionary[type] = handler;
        }

        _frozenDictionary = _dictionary.ToFrozenDictionary();

        _concurrentDictionary = new ConcurrentDictionary<Type, Func<object, Guid>>();
        foreach (var (type, handler) in entries)
        {
            _concurrentDictionary[type] = handler;
        }

        // Build hash table for span access benchmark
        var types = new Type[20];
        for (int i = 0; i < 20; i++)
            types[i] = _messages[i].GetType();

        BuildTable(types, _handler, out _table, out _mask, out _shift, out _multiplier, out _);

        var handleDict = new Dictionary<nint, Func<object, Guid>>(20);
        foreach (var t in types) handleDict[t.TypeHandle.Value] = _handler;
        _handleFrozenDictionary = handleDict.ToFrozenDictionary();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetSlot(nint handle, int mask, int shift, ulong multiplier)
    {
        unchecked
        {
            return (int)(((ulong)handle * multiplier) >> shift) & mask;
        }
    }

    private static void BuildTable(
        Type[] types,
        Func<object, Guid> handler,
        out Entry[] table,
        out int mask,
        out int shift,
        out ulong multiplier,
        out int maxProbeLength)
    {
        int n = types.Length;
        int minBits = (int)Math.Ceiling(Math.Log2(n)) + 1;

        Entry[]? bestTable = null;
        int bestMask = 0, bestShift = 0;
        ulong bestMultiplier = 0;
        int bestMaxProbe = int.MaxValue;

        for (int bits = minBits; bits <= minBits + 4; bits++)
        {
            int tableSize = 1 << bits;
            int tableMask = tableSize - 1;
            int tableShift = 64 - bits;

            for (ulong m = 0x9E3779B97F4A7C15UL; m < 0x9E3779B97F4A7C15UL + 100_000; m += 2)
            {
                var test = new Entry[tableSize];
                int localMaxProbe = 0;
                bool ok = true;

                foreach (var t in types)
                {
                    nint h = t.TypeHandle.Value;
                    int slot;
                    unchecked
                    {
                        slot = (int)(((ulong)h * m) >> tableShift) & tableMask;
                    }
                    int probe = 0;

                    while (test[slot].Handler != null)
                    {
                        slot = (slot + 1) & tableMask;
                        probe++;
                        if (probe >= tableSize) { ok = false; break; }
                    }

                    if (!ok) break;
                    test[slot] = new Entry { TypeHandle = h, Handler = handler };
                    localMaxProbe = Math.Max(localMaxProbe, probe);
                }

                if (!ok) continue;

                if (localMaxProbe == 0)
                {
                    table = test;
                    mask = tableMask;
                    shift = tableShift;
                    multiplier = m;
                    maxProbeLength = 0;
                    return;
                }

                if (localMaxProbe < bestMaxProbe)
                {
                    bestTable = test;
                    bestMask = tableMask;
                    bestShift = tableShift;
                    bestMultiplier = m;
                    bestMaxProbe = localMaxProbe;
                    if (bestMaxProbe <= 1) break;
                }
            }

            if (bestMaxProbe <= 1) break;
        }

        table = bestTable!;
        mask = bestMask;
        shift = bestShift;
        multiplier = bestMultiplier;
        maxProbeLength = bestMaxProbe;
    }

    [Benchmark(Baseline = true)]
    public Guid SwitchType()
    {
        return _targetMessage switch
        {
            Msg00 m => _handler(m),
            Msg01 m => _handler(m),
            Msg02 m => _handler(m),
            Msg03 m => _handler(m),
            Msg04 m => _handler(m),
            Msg05 m => _handler(m),
            Msg06 m => _handler(m),
            Msg07 m => _handler(m),
            Msg08 m => _handler(m),
            Msg09 m => _handler(m),
            Msg10 m => _handler(m),
            Msg11 m => _handler(m),
            Msg12 m => _handler(m),
            Msg13 m => _handler(m),
            Msg14 m => _handler(m),
            Msg15 m => _handler(m),
            Msg16 m => _handler(m),
            Msg17 m => _handler(m),
            Msg18 m => _handler(m),
            Msg19 m => _handler(m),
            _ => throw new InvalidOperationException()
        };
    }

    [Benchmark]
    public Guid SwitchGetType()
    {
        var type = _targetMessage.GetType();
        if (type == typeof(Msg00)) return _handler(_targetMessage);
        if (type == typeof(Msg01)) return _handler(_targetMessage);
        if (type == typeof(Msg02)) return _handler(_targetMessage);
        if (type == typeof(Msg03)) return _handler(_targetMessage);
        if (type == typeof(Msg04)) return _handler(_targetMessage);
        if (type == typeof(Msg05)) return _handler(_targetMessage);
        if (type == typeof(Msg06)) return _handler(_targetMessage);
        if (type == typeof(Msg07)) return _handler(_targetMessage);
        if (type == typeof(Msg08)) return _handler(_targetMessage);
        if (type == typeof(Msg09)) return _handler(_targetMessage);
        if (type == typeof(Msg10)) return _handler(_targetMessage);
        if (type == typeof(Msg11)) return _handler(_targetMessage);
        if (type == typeof(Msg12)) return _handler(_targetMessage);
        if (type == typeof(Msg13)) return _handler(_targetMessage);
        if (type == typeof(Msg14)) return _handler(_targetMessage);
        if (type == typeof(Msg15)) return _handler(_targetMessage);
        if (type == typeof(Msg16)) return _handler(_targetMessage);
        if (type == typeof(Msg17)) return _handler(_targetMessage);
        if (type == typeof(Msg18)) return _handler(_targetMessage);
        if (type == typeof(Msg19)) return _handler(_targetMessage);
        throw new InvalidOperationException();
    }

    [Benchmark]
    public Guid DictionaryLookup()
    {
        return _dictionary[_targetMessage.GetType()](_targetMessage);
    }

    [Benchmark]
    public Guid FrozenDictionaryLookup()
    {
        return _frozenDictionary[_targetMessage.GetType()](_targetMessage);
    }

    [Benchmark]
    public Guid ConcurrentDictionaryLookup()
    {
        return _concurrentDictionary[_targetMessage.GetType()](_targetMessage);
    }

    [Benchmark]
    public Guid Span_Access()
    {
        nint handle = _targetMessage.GetType().TypeHandle.Value;
        int slot = GetSlot(handle, _mask, _shift, _multiplier);

        Span<Entry> span = _table.AsSpan();

        ref Entry entry = ref span[slot];
        if (entry.TypeHandle == handle)
            return entry.Handler(_targetMessage);

        while (true)
        {
            slot = (slot + 1) & _mask;
            entry = ref span[slot];
            if (entry.TypeHandle == handle)
                return entry.Handler(_targetMessage);
        }
    }

    [Benchmark]
    public Guid UnsafeAdd_NoBoundsCheck()
    {
        nint handle = _targetMessage.GetType().TypeHandle.Value;
        int slot = GetSlot(handle, _mask, _shift, _multiplier);

        ref Entry origin = ref MemoryMarshal.GetArrayDataReference(_table);

        ref Entry entry = ref Unsafe.Add(ref origin, slot);
        if (entry.TypeHandle == handle)
            return entry.Handler(_targetMessage);

        while (true)
        {
            slot = (slot + 1) & _mask;
            entry = ref Unsafe.Add(ref origin, slot);
            if (entry.TypeHandle == handle)
                return entry.Handler(_targetMessage);
        }
    }

    [Benchmark]
    public Guid HandleFrozenDictionary()
    {
        return _handleFrozenDictionary[_targetMessage.GetType().TypeHandle.Value](_targetMessage);
    }
}
