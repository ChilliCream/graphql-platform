namespace HotChocolate.Caching.Memory;

public class CacheTests
{
    [Fact]
    public void Fill_Cache_Up()
    {
        // arrange
        var cache = new Cache<string>(10);

        for (var i = 0; i < 9; i++)
        {
            var item = i.ToString();
            cache.GetOrCreate(item, _ => item);
        }

        // assert
        var value = cache.GetOrCreate("9", _ => "9");

        // assert
        Assert.Equal("9", value);
        Assert.Equal(10, cache.Capacity);
        Assert.Equal(10, cache.Count);

        Assert.Collection(
            cache.GetKeys(),
            key => Assert.Equal("0", key),
            key => Assert.Equal("1", key),
            key => Assert.Equal("2", key),
            key => Assert.Equal("3", key),
            key => Assert.Equal("4", key),
            key => Assert.Equal("5", key),
            key => Assert.Equal("6", key),
            key => Assert.Equal("7", key),
            key => Assert.Equal("8", key),
            key => Assert.Equal("9", key));
    }

    [Fact]
    public void Add_More_Items_To_The_Cache_Than_We_Have_Space()
    {
        // arrange
        var cache = new Cache<string>(10);

        // fill up all slots.
        for (var i = 0; i < 10; i++)
        {
            var item = i.ToString();
            cache.GetOrCreate(item, _ => item);
        }

        // assert
        // Adds and 11th item that will cause the eviction of an item.
        var value = cache.GetOrCreate("10", _ => "10");

        // assert
        Assert.Equal("10", value);
        Assert.Equal(10, cache.Capacity);
        Assert.Equal(10, cache.Count);

        Assert.Collection(
            cache.GetKeys(),
            // item 0 was evicted from slot 0
            key => Assert.Equal("10", key),
            key => Assert.Equal("1", key),
            key => Assert.Equal("2", key),
            key => Assert.Equal("3", key),
            key => Assert.Equal("4", key),
            key => Assert.Equal("5", key),
            key => Assert.Equal("6", key),
            key => Assert.Equal("7", key),
            key => Assert.Equal("8", key),
            key => Assert.Equal("9", key));
    }

    [Fact]
    public void Evict_Items_That_Were_Not_Recently_Access_When_Cache_Is_Full()
    {
        // arrange
        var cache = new Cache<string>(10);
        cache.GetOrCreate("a", _ => "a");
        cache.GetOrCreate("b", _ => "b");
        cache.GetOrCreate("c", _ => "c");
        cache.GetOrCreate("d", _ => "d");
        cache.GetOrCreate("e", _ => "e");
        cache.GetOrCreate("f", _ => "f");
        cache.GetOrCreate("g", _ => "g");
        cache.GetOrCreate("h", _ => "h");
        cache.GetOrCreate("i", _ => "i");
        cache.GetOrCreate("j", _ => "j");
        cache.GetOrCreate("x", _ => "x");

        // act
        Assert.True(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _));
        Assert.True(cache.TryGet("d", out _));
        cache.GetOrCreate("y", _ => "y");
        Assert.True(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _));
        Assert.True(cache.TryGet("d", out _));
        cache.GetOrCreate("z", _ => "z");

        // assert
        Assert.Collection(
            cache.GetKeys(),
            key => Assert.Equal("x", key),
            key => Assert.Equal("b", key),
            key => Assert.Equal("c", key),
            key => Assert.Equal("d", key),
            key => Assert.Equal("y", key),
            key => Assert.Equal("z", key),
            key => Assert.Equal("g", key),
            key => Assert.Equal("h", key),
            key => Assert.Equal("i", key),
            key => Assert.Equal("j", key));
    }

    [Fact]
    public void TryGet_Hit_IncrementsHitCounter()
    {
        var diag = new TestDiagnostics();
        var cache = new Cache<int>(capacity: 16, diagnostics: diag);

        cache.GetOrCreate("a", _ => 42); // first call = miss
        Assert.True(cache.TryGet("a", out _)); // second call = hit

        Assert.Equal(1, diag.Hits);
        Assert.Equal(1, diag.Misses);
        Assert.Equal(0, diag.Evictions);
    }

    [Fact]
    public void GetOrCreate_HitVsMiss_CountsCorrectly()
    {
        var diag = new TestDiagnostics();
        var cache = new Cache<int>(capacity: 8, diagnostics: diag);

        // first request → miss
        var v1 = cache.GetOrCreate("x", _ => 123);
        // second request → hit
        var v2 = cache.GetOrCreate("x", _ => 456);

        Assert.Equal(123, v1);
        Assert.Equal(123, v2); // the factory isn't called second time

        Assert.Equal(1, diag.Misses);
        Assert.Equal(1, diag.Hits);
    }

    [Fact]
    public void Eviction_IsRecorded_WhenRingIsFull()
    {
        var diag = new TestDiagnostics();
        var cache = new Cache<int>(capacity: 2, diagnostics: diag);

        cache.GetOrCreate("a", _ => 1); // fill slot 0
        cache.GetOrCreate("b", _ => 2); // fill slot 1
        cache.GetOrCreate("c", _ => 3); // forces eviction of one entry

        Assert.Equal(1, diag.Evictions);
        Assert.Equal(2, cache.Count); // still limited by capacity
    }

    [Fact]
    public void Gauges_ReportSizeAndCapacity()
    {
        var diag = new TestDiagnostics();
        var cache = new Cache<string>(capacity: 4, diagnostics: diag);

        cache.GetOrCreate("k1", _ => "x");
        cache.GetOrCreate("k2", _ => "y");

        Assert.NotNull(diag.SizeGauge);
        Assert.NotNull(diag.CapacityGauge);

        Assert.Equal(2, diag.SizeGauge());
        Assert.Equal(4, diag.CapacityGauge());
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenKeyIsAbsent()
    {
        var diag = new TestDiagnostics();
        var cache = new Cache<int>(capacity: 8, diagnostics: diag);

        var found = cache.TryGet("unknown", out _);

        Assert.False(found);
        Assert.Equal(1, diag.Misses); // miss path recorded
        Assert.Equal(0, diag.Hits);
    }

    [Fact]
    public void TryGet_Should_Throw_When_KeyIsNull()
    {
        // arrange
        var cache = new Cache<string>(capacity: 8);

        // act
        // assert
        Assert.Throws<ArgumentNullException>(() => cache.TryGet(null!, out _));
    }

    [Fact]
    public void TryGet_Should_Throw_When_KeyIsEmpty()
    {
        // arrange
        var cache = new Cache<string>(capacity: 8);

        // act
        // assert
        Assert.Throws<ArgumentException>(() => cache.TryGet(string.Empty, out _));
    }

    [Fact]
    public void TryAdd_Should_Throw_When_KeyIsNull()
    {
        // arrange
        var cache = new Cache<string>(capacity: 8);

        // act
        // assert
        Assert.Throws<ArgumentNullException>(() => cache.TryAdd(null!, "value"));
    }

    [Fact]
    public void TryAdd_Should_Throw_When_KeyIsEmpty()
    {
        // arrange
        var cache = new Cache<string>(capacity: 8);

        // act
        // assert
        Assert.Throws<ArgumentException>(() => cache.TryAdd(string.Empty, "value"));
    }

    [Fact]
    public void GetOrCreate_Should_Throw_When_KeyIsNull()
    {
        // arrange
        var cache = new Cache<string>(capacity: 8);

        // act
        // assert
        Assert.Throws<ArgumentNullException>(() => cache.GetOrCreate(null!, _ => "value"));
    }

    [Fact]
    public void GetOrCreate_Should_Throw_When_KeyIsEmpty()
    {
        // arrange
        var cache = new Cache<string>(capacity: 8);

        // act
        // assert
        Assert.Throws<ArgumentException>(() => cache.GetOrCreate(string.Empty, _ => "value"));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public void TryGet_Should_ReturnTrue_When_KeyExists_UsingSpan()
    {
        // arrange
        var diag = new TestDiagnostics();
        var cache = new Cache<string>(capacity: 8, diagnostics: diag);
        cache.TryAdd("key", "value");

        // act
        var found = cache.TryGet("key".AsSpan(), out var value);

        // assert
        Assert.True(found);
        Assert.Equal("value", value);
        Assert.Equal(1, diag.Hits);
        Assert.Equal(1, diag.Misses); // from setup TryAdd
    }

    [Fact]
    public void TryGet_Should_ReturnFalse_When_KeyDoesNotExist_UsingSpan()
    {
        // arrange
        var diag = new TestDiagnostics();
        var cache = new Cache<string>(capacity: 8, diagnostics: diag);

        // act
        var found = cache.TryGet("missing".AsSpan(), out var value);

        // assert
        Assert.False(found);
        Assert.Null(value);
        Assert.Equal(1, diag.Misses);
        Assert.Equal(0, diag.Hits);
    }

    [Fact]
    public void TryAdd_Should_AddEntry_When_KeyDoesNotExist_UsingSpan()
    {
        // arrange
        var diag = new TestDiagnostics();
        var cache = new Cache<string>(capacity: 8, diagnostics: diag);

        // act
        cache.TryAdd("key".AsSpan(), "value");

        // assert
        Assert.True(cache.TryGet("key", out var value));
        Assert.Equal("value", value);
        Assert.Equal(1, diag.Misses); // span TryAdd fell through to string TryAdd factory
        Assert.Equal(1, diag.Hits); // the string TryGet in the assertion
    }

    [Fact]
    public void TryAdd_Should_NotOverwrite_When_KeyExists_UsingSpan()
    {
        // arrange
        var diag = new TestDiagnostics();
        var cache = new Cache<string>(capacity: 8, diagnostics: diag);
        cache.TryAdd("key", "original");

        // act
        cache.TryAdd("key".AsSpan(), "replacement");

        // assert
        Assert.True(cache.TryGet("key", out var value));
        Assert.Equal("original", value);
        Assert.Equal(1, diag.Misses); // only from setup; span fast path records nothing
        Assert.Equal(1, diag.Hits); // the string TryGet in the assertion
    }

    [Fact]
    public void GetOrCreate_Should_ReturnExisting_When_KeyExists_UsingSpan()
    {
        // arrange
        var diag = new TestDiagnostics();
        var cache = new Cache<string>(capacity: 8, diagnostics: diag);
        cache.TryAdd("key", "existing");
        var factoryInvocations = 0;

        // act
        var value = cache.GetOrCreate(
            "key".AsSpan(),
            _ =>
            {
                factoryInvocations++;
                return "created";
            });

        // assert
        Assert.Equal("existing", value);
        Assert.Equal(0, factoryInvocations);
        Assert.Equal(1, diag.Hits);
        Assert.Equal(1, diag.Misses); // from setup TryAdd
    }

    [Fact]
    public void GetOrCreate_Should_InvokeFactory_When_KeyDoesNotExist_UsingSpan()
    {
        // arrange
        var diag = new TestDiagnostics();
        var cache = new Cache<string>(capacity: 8, diagnostics: diag);
        var factoryInvocations = 0;

        // act
        var value = cache.GetOrCreate(
            "key".AsSpan(),
            _ =>
            {
                factoryInvocations++;
                return "created";
            });

        // assert
        Assert.Equal("created", value);
        Assert.Equal(1, factoryInvocations);
        Assert.True(cache.TryGet("key".AsSpan(), out var stored));
        Assert.Equal("created", stored);
        $"Hits: {diag.Hits}, Misses: {diag.Misses}".MatchInlineSnapshot("Hits: 1, Misses: 1");
    }

    [Fact]
    public void GetOrCreate_WithState_Should_PassStateToFactory_UsingSpan()
    {
        // arrange
        var diag = new TestDiagnostics();
        var cache = new Cache<string>(capacity: 8, diagnostics: diag);
        string? receivedKey = null;
        var receivedState = 0;

        // act
        var value = cache.GetOrCreate(
            "key".AsSpan(),
            (k, s) =>
            {
                receivedKey = k;
                receivedState = s;
                return $"{k}:{s}";
            },
            42);

        // assert
        Assert.Equal("key:42", value);
        Assert.Equal("key", receivedKey);
        Assert.Equal(42, receivedState);
        Assert.Equal(1, diag.Misses);
        Assert.Equal(0, diag.Hits);
    }

    [Fact]
    public void SpanAndStringLookups_Should_SeeSameEntries()
    {
        // arrange
        var diag = new TestDiagnostics();
        var cache = new Cache<string>(capacity: 8, diagnostics: diag);
        cache.TryAdd("via-string", "a");
        cache.TryAdd("via-span".AsSpan(), "b");

        // act
        var spanFoundString = cache.TryGet("via-string".AsSpan(), out var spanValue);
        var stringFoundSpan = cache.TryGet("via-span", out var stringValue);

        // assert
        Assert.True(spanFoundString);
        Assert.Equal("a", spanValue);
        Assert.True(stringFoundSpan);
        Assert.Equal("b", stringValue);
        $"Hits: {diag.Hits}, Misses: {diag.Misses}".MatchInlineSnapshot("Hits: 2, Misses: 2");
    }

    [Fact]
    public void TryGet_Should_Throw_When_KeyIsEmpty_UsingSpan()
    {
        // arrange
        var cache = new Cache<string>(capacity: 8);

        // act
        // assert
        Assert.Throws<ArgumentException>(() => cache.TryGet(ReadOnlySpan<char>.Empty, out _));
    }

    [Fact]
    public void TryAdd_Should_Throw_When_KeyIsEmpty_UsingSpan()
    {
        // arrange
        var cache = new Cache<string>(capacity: 8);

        // act
        // assert
        Assert.Throws<ArgumentException>(() => cache.TryAdd(ReadOnlySpan<char>.Empty, "value"));
    }

    [Fact]
    public void GetOrCreate_Should_Throw_When_KeyIsEmpty_UsingSpan()
    {
        // arrange
        var cache = new Cache<string>(capacity: 8);

        // act
        // assert
        Assert.Throws<ArgumentException>(() => cache.GetOrCreate(ReadOnlySpan<char>.Empty, _ => "value"));
    }

    [Fact]
    public void GetOrCreate_WithState_Should_Throw_When_KeyIsEmpty_UsingSpan()
    {
        // arrange
        var cache = new Cache<string>(capacity: 8);

        // act
        // assert
        Assert.Throws<ArgumentException>(() => cache.GetOrCreate(ReadOnlySpan<char>.Empty, (k, s) => $"{k}:{s}", 1));
    }
#endif

    private sealed class TestDiagnostics : CacheDiagnostics
    {
        public int Hits;
        public int Misses;
        public int Evictions;
        public Func<long>? SizeGauge;
        public Func<long>? CapacityGauge;

        public override void Hit() => Hits++;

        public override void Miss() => Misses++;

        public override void Evict() => Evictions++;

        public override void RegisterSizeGauge(Func<long> sizeProvider)
            => SizeGauge = sizeProvider;

        public override void RegisterCapacityGauge(Func<long> capacityProvider)
            => CapacityGauge = capacityProvider;
    }
}
