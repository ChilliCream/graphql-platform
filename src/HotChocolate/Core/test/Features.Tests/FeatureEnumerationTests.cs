using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace HotChocolate.Features;

public sealed class FeatureEnumerationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetEnumerator_Should_PreserveOverrideAndFirstDefaultOrder_When_DefaultsRepeatKeys(bool pooled)
    {
        // arrange
        var own = new object();
        var first = new object();
        var last = new object();
        var defaults = new SequenceFeatures(
            [new(typeof(string), new object()), new(typeof(int), first),
                new(typeof(int), new object()), new(typeof(long), last)], false);
        var collection = Create(pooled, defaults);
        collection[typeof(string)] = own;

        // act
        var actual = collection.ToArray();

        // assert
        Assert.Equal<KeyValuePair<Type, object>>(
            [new(typeof(string), own), new(typeof(int), first), new(typeof(long), last)], actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetEnumerator_Should_KeepExclusionSnapshot_When_LocalKeyIsAddedDuringDefaults(bool pooled)
    {
        // arrange
        var own = new object();
        var first = new object();
        var last = new object();
        var collection = Create(pooled, new SequenceFeatures(
            [new(typeof(int), first), new(typeof(long), last)], false));
        collection[typeof(string)] = own;
        using var iterator = collection.GetEnumerator();
        var actual = new List<KeyValuePair<Type, object>>();

        // act
        Take(iterator, actual);
        Take(iterator, actual);
        collection[typeof(long)] = new object();
        TakeRemaining(iterator, actual);

        // assert
        Assert.Equal<KeyValuePair<Type, object>>(
            [new(typeof(string), own), new(typeof(int), first), new(typeof(long), last)], actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetEnumerator_Should_SeedCurrentLocalKeys_When_LocalKeyIsRemovedBeforeDefaults(bool pooled)
    {
        // arrange
        var own = new object();
        var fallback = new object();
        var collection = Create(pooled, new SequenceFeatures([new(typeof(string), fallback)], false));
        collection[typeof(string)] = own;
        using var iterator = collection.GetEnumerator();
        var actual = new List<KeyValuePair<Type, object>>();

        // act
        Take(iterator, actual);
        collection[typeof(string)] = null;
        TakeRemaining(iterator, actual);

        // assert
        Assert.Equal<KeyValuePair<Type, object>>([new(typeof(string), own), new(typeof(string), fallback)], actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetEnumerator_Should_Throw_When_LocalKeyIsAddedDuringLocalEnumeration(bool pooled)
    {
        // arrange
        var collection = Create(pooled, FeatureCollection.Empty);
        collection[typeof(string)] = new object();
        collection[typeof(int)] = new object();
        using var iterator = collection.GetEnumerator();
        iterator.MoveNext();

        // act
        collection[typeof(long)] = new object();

        // assert
        Assert.Throws<InvalidOperationException>(() => iterator.MoveNext());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetEnumerator_Should_DisposeDefaults_When_ConsumerStopsEarly(bool pooled)
    {
        // arrange
        var defaults = new SequenceFeatures([new(typeof(int), new object()), new(typeof(long), new object())], false);
        var collection = Create(pooled, defaults);
        collection[typeof(string)] = new object();

        // act
        using (var iterator = collection.GetEnumerator())
        {
            iterator.MoveNext();
            iterator.MoveNext();
        }

        // assert
        Assert.Equal(1, defaults.Disposals);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetEnumerator_Should_PropagateAndDispose_When_DefaultsThrow(bool pooled)
    {
        // arrange
        var defaults = new SequenceFeatures([new(typeof(int), new object())], true);
        var collection = Create(pooled, defaults);
        collection[typeof(string)] = new object();
        using var iterator = collection.GetEnumerator();
        iterator.MoveNext();
        iterator.MoveNext();

        // act
        var error = Record.Exception(() => iterator.MoveNext());

        // assert
        Assert.IsType<SequenceFailureException>(error);
        Assert.Equal(1, defaults.Disposals);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetEnumerator_Should_KeepIndependentState_When_EnumerationsInterleave(bool pooled)
    {
        // arrange
        var own = new object();
        var fallback = new object();
        var collection = Create(pooled, new SequenceFeatures([new(typeof(int), fallback)], false));
        collection[typeof(string)] = own;
        using var first = collection.GetEnumerator();
        using var second = collection.GetEnumerator();
        var firstValues = new List<KeyValuePair<Type, object>>();
        var secondValues = new List<KeyValuePair<Type, object>>();

        // act
        Take(first, firstValues);
        Take(second, secondValues);
        TakeRemaining(first, firstValues);
        TakeRemaining(second, secondValues);

        // assert
        Assert.Equal<KeyValuePair<Type, object>>([new(typeof(string), own), new(typeof(int), fallback)], firstValues);
        Assert.Equal(firstValues, secondValues);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void GetEnumerator_Should_ThrowNullReference_When_DeduplicatedDefaultsContainNullKey(bool pooled, bool local)
    {
        // arrange
        var collection = Create(pooled, new SequenceFeatures([new(null!, new object())], false));
        if (local)
        {
            collection[typeof(string)] = new object();
        }

        // act
        var error = Record.Exception(() => collection.ToArray());

        // assert
        Assert.IsType<NullReferenceException>(error);
    }

    [Fact]
    public void GetEnumerator_Should_PassDefaultsThrough_When_LocalDictionaryWasNeverCreated()
    {
        // arrange
        KeyValuePair<Type, object>[] values =
            [new(typeof(string), new object()), new(typeof(string), new object()), new(null!, new object())];
        var collection = new FeatureCollection(new SequenceFeatures(values, false));

        // act
        var actual = collection.ToArray();

        // assert
        Assert.Equal(values, actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetEnumerator_Should_DeduplicateDefaults_When_LocalDictionaryWasEmptied(bool pooled)
    {
        // arrange
        var first = new object();
        var collection = Create(pooled, new SequenceFeatures(
            [new(typeof(int), first), new(typeof(int), new object())], false));
        collection[typeof(string)] = new object();
        collection[typeof(string)] = null;

        // act
        var actual = collection.ToArray();

        // assert
        Assert.Equal<KeyValuePair<Type, object>>([new(typeof(int), first)], actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetEnumerator_Should_UseTypeEqualsOverload_When_CustomTypeKeysCompareDifferently(bool pooled)
    {
        // arrange
        var firstKey = new FeatureKeyType("same");
        var duplicateKey = new FeatureKeyType("same");
        var first = new object();
        var collection = Create(pooled, new SequenceFeatures(
            [new(firstKey, first), new(duplicateKey, new object())], false));
        collection[typeof(string)] = new object();

        // act
        var actual = collection.ToArray();

        // assert
        Assert.Equal(2, actual.Length);
        Assert.Same(firstKey, actual[1].Key);
        Assert.Same(first, actual[1].Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetEnumerator_Should_PreserveNestedOverrides_When_DefaultsArePooled(bool pooled)
    {
        // arrange
        var middleValue = new object();
        var rootValue = new object();
        var own = new object();
        var root = new FeatureCollection();
        root[typeof(string)] = new object();
        root[typeof(int)] = rootValue;
        var middle = new PooledFeatureCollection(new object());
        middle.Initialize(root);
        middle[typeof(string)] = middleValue;
        var collection = Create(pooled, middle);
        collection[typeof(long)] = own;

        // act
        var actual = collection.ToArray();

        // assert
        Assert.Equal<KeyValuePair<Type, object>>(
            [new(typeof(long), own), new(typeof(string), middleValue), new(typeof(int), rootValue)], actual);
    }

    private static IFeatureCollection Create(bool pooled, IFeatureCollection defaults)
    {
        if (!pooled)
        {
            return new FeatureCollection(defaults);
        }
        var collection = new PooledFeatureCollection(new object());
        collection.Initialize(defaults);
        return collection;
    }

    private static void Take(IEnumerator<KeyValuePair<Type, object>> iterator, List<KeyValuePair<Type, object>> values)
    {
        if (!iterator.MoveNext())
        {
            throw new InvalidOperationException("Expected another feature.");
        }
        values.Add(iterator.Current);
    }

    private static void TakeRemaining(IEnumerator<KeyValuePair<Type, object>> iterator, List<KeyValuePair<Type, object>> values)
    {
        while (iterator.MoveNext())
        {
            values.Add(iterator.Current);
        }
    }

    private sealed class FeatureKeyType(string group) : TypeDelegator(typeof(object))
    {
        private readonly string _group = group;

        public override bool Equals(Type? other) => other is FeatureKeyType key && _group == key._group;

        public override bool Equals(object? other) => ReferenceEquals(this, other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_group);
    }

    private sealed class SequenceFailureException : Exception;

    private sealed class SequenceFeatures(KeyValuePair<Type, object>[] values, bool failAfterItems) : IFeatureCollection
    {
        public int Disposals { get; private set; }

        public bool IsReadOnly => true;

        public bool IsEmpty => values.Length == 0;

        public int Revision => 0;

        public object? this[Type key]
        {
            get => values.FirstOrDefault(pair => pair.Key == key).Value;
            set => throw new NotSupportedException();
        }

        public bool TryGet<TFeature>([NotNullWhen(true)] out TFeature? feature)
        {
            feature = this[typeof(TFeature)] is TFeature result ? result : default;
            return feature is not null;
        }

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            try
            {
                foreach (var pair in values)
                {
                    yield return pair;
                }
                if (failAfterItems)
                {
                    throw new SequenceFailureException();
                }
            }
            finally
            {
                Disposals++;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
