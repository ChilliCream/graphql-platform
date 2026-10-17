using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Execution;

namespace HotChocolate.Features;

public sealed class FeatureSnapshotTests
{
    private static readonly Type[] s_keys =
    [
        typeof(object), typeof(string), typeof(int), typeof(long), typeof(byte), typeof(short),
        typeof(float), typeof(double), typeof(decimal), typeof(bool), typeof(char), typeof(DateTime),
        typeof(Guid), typeof(TimeSpan), typeof(Uri), typeof(Version), typeof(Exception), typeof(Type),
        typeof(Stream), typeof(TextReader), typeof(TextWriter), typeof(Random), typeof(Thread),
        typeof(Task), typeof(CancellationToken), typeof(MemoryStream), typeof(Delegate), typeof(Array),
        typeof(Enum), typeof(Math), typeof(Convert), typeof(FeatureSnapshotTests)
    ];

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(32)]
    public void ToReadOnly_Should_IsolateSnapshot_When_SourceIsChanged(int count)
    {
        // arrange
        var source = new FeatureCollection();
        foreach (var key in s_keys.Take(count))
        {
            source[key] = new object();
        }
        var expected = source.OrderBy(pair => pair.Key.FullName, StringComparer.Ordinal).ToArray();
        var snapshot = source.ToReadOnly();

        // act
        source[s_keys[0]] = null;
        source[s_keys[1]] = new object();
        source[typeof(FeatureCollection)] = new object();

        // assert
        Assert.True(snapshot.IsReadOnly);
        Assert.Equal(expected, snapshot.OrderBy(pair => pair.Key.FullName, StringComparer.Ordinal).ToArray());
        Assert.Equal(expected.Select(pair => pair.Value), expected.Select(pair => snapshot[pair.Key]));
        Assert.Throws<NotSupportedException>(() => snapshot[s_keys[0]] = new object());
    }

    [Fact]
    public void ToReadOnly_Should_PreserveDictionaryOrder_When_RemovedSlotsAreReused()
    {
        // arrange
        var source = new FeatureCollection(32);
        source[typeof(string)] = new object();
        source[typeof(int)] = new object();
        source[typeof(long)] = new object();
        source[typeof(int)] = null;
        source[typeof(byte)] = new object();
        var expected = source.ToArray();

        // act
        var snapshot = source.ToReadOnly();

        // assert
        Assert.Equal(expected, snapshot.ToArray());
        Assert.Equal(expected, source.ToArray());
        Assert.Equal(source.Revision, snapshot.Revision);
    }

    [Fact]
    public void Build_Should_KeepPriorSnapshot_When_BuilderAndSourceAreReused()
    {
        // arrange
        var builder = OperationRequestBuilder.New().SetDocument("{ first }");
        var firstValue = new object();
        var secondValue = new object();
        var source = builder.Features;
        source[typeof(string)] = firstValue;
        using var first = builder.Build();

        // act
        source[typeof(string)] = new object();
        builder.SetDocument("{ second }");
        builder.Features[typeof(string)] = secondValue;
        using var second = builder.Build();

        // assert
        Assert.Equal<KeyValuePair<Type, object>>([new(typeof(string), firstValue)], first.Features.ToArray());
        Assert.Equal<KeyValuePair<Type, object>>([new(typeof(string), secondValue)], second.Features.ToArray());
        Assert.Empty(builder.Features);
    }

    [Fact]
    public void ToReadOnly_Should_CaptureRevisionAfterSealing_When_SealChangesSource()
    {
        // arrange
        var source = new FeatureCollection();
        var value = new SealableFeature(() => source[typeof(string)] = new object(), false);
        source[typeof(SealableFeature)] = value;

        // act
        var snapshot = source.ToReadOnly();

        // assert
        Assert.True(value.IsReadOnly);
        Assert.Equal(1, value.SealCalls);
        Assert.Equal(source.Revision, snapshot.Revision);
        Assert.Equal<KeyValuePair<Type, object>>([new(typeof(SealableFeature), value)], snapshot.ToArray());
    }

    [Fact]
    public void ToReadOnly_Should_KeepSealedFeatures_When_ValueWasAlreadySealed()
    {
        // arrange
        var value = new SealableFeature(static () => { }, true);
        var source = new FeatureCollection();
        source[typeof(SealableFeature)] = value;

        // act
        var snapshot = source.ToReadOnly();

        // assert
        Assert.Equal(0, value.SealCalls);
        Assert.Same(value, snapshot.Get<SealableFeature>());
    }

    [Fact]
    public void ToReadOnly_Should_PropagateSealFailure_When_FeatureThrows()
    {
        // arrange
        var source = new FeatureCollection();
        var value = new SealableFeature(static () => throw new SealFailureException(), false);
        source[typeof(SealableFeature)] = value;
        var revision = source.Revision;

        // act
        var error = Record.Exception(() => source.ToReadOnly());

        // assert
        Assert.IsType<SealFailureException>(error);
        Assert.Equal(1, value.SealCalls);
        Assert.Equal(revision, source.Revision);
        Assert.Equal<KeyValuePair<Type, object>>([new(typeof(SealableFeature), value)], source.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToReadOnly_Should_KeepEmptyBehavior_When_DictionaryWasCreatedOrAbsent(bool createDictionary)
    {
        // arrange
        var source = new FeatureCollection();
        if (createDictionary)
        {
            source[typeof(string)] = new object();
            source[typeof(string)] = null;
        }

        // act
        var empty = source.ToReadOnly();
        var direct = new ReadOnlyFeatureCollection(source);

        // assert
        Assert.Same(FeatureCollection.Empty, empty);
        Assert.Empty(direct);
        Assert.Equal(source.Revision, direct.Revision);
    }

    [Fact]
    public void ToReadOnly_Should_KeepExistingSnapshot_When_SourceIsAlreadyReadOnly()
    {
        // arrange
        var source = new FeatureCollection();
        source[typeof(string)] = new object();
        var snapshot = source.ToReadOnly();

        // act
        var repeated = snapshot.ToReadOnly();

        // assert
        Assert.Same(snapshot, repeated);
    }

    [Fact]
    public void ToReadOnly_Should_PreserveDefaultSnapshots_When_DefaultsAndOverridesChange()
    {
        // arrange
        var root = new FeatureCollection();
        root[typeof(string)] = new object();
        var inherited = new object();
        root[typeof(long)] = inherited;
        var source = new FeatureCollection(root);
        var own = new object();
        source[typeof(string)] = own;
        var snapshot = source.ToReadOnly();

        // act
        source[typeof(string)] = new object();
        root[typeof(long)] = new object();

        // assert
        Assert.Equal<KeyValuePair<Type, object>>([new(typeof(string), own), new(typeof(long), inherited)], snapshot.ToArray());
    }

    [Fact]
    public void ToReadOnly_Should_PreserveCustomTypeEquality_When_EquivalentKeysReplaceValues()
    {
        // arrange
        var source = new FeatureCollection();
        var first = new FeatureKeyType("same");
        var equivalent = new FeatureKeyType("same");
        var replacement = new object();
        source[first] = new object();
        source[equivalent] = replacement;

        // act
        var snapshot = source.ToReadOnly();

        // assert
        Assert.Single(snapshot);
        Assert.Same(replacement, snapshot[new FeatureKeyType("same")]);
        Assert.Same(first, snapshot.Single().Key);
    }

    [Fact]
    public void ToReadOnly_Should_UseLastCustomDefaultValue_When_SourceRepeatsKeys()
    {
        // arrange
        var first = new SealableFeature(static () => { }, false);
        var last = new SealableFeature(static () => { }, false);
        var source = new SequenceFeatures([new(typeof(SealableFeature), first), new(typeof(SealableFeature), last)]);

        // act
        var snapshot = source.ToReadOnly();

        // assert
        Assert.Equal<KeyValuePair<Type, object>>([new(typeof(SealableFeature), last)], snapshot.ToArray());
        Assert.Equal<int>([0, 1], [first.SealCalls, last.SealCalls]);
    }

    private sealed class SealableFeature(Action onSeal, bool isReadOnly) : ISealable
    {
        public int SealCalls { get; private set; }

        public bool IsReadOnly { get; private set; } = isReadOnly;

        public void Seal()
        {
            SealCalls++;
            onSeal();
            IsReadOnly = true;
        }
    }

    private sealed class SealFailureException : Exception;

    private sealed class FeatureKeyType(string group) : TypeDelegator(typeof(object))
    {
        private readonly string _group = group;

        public override bool Equals(Type? other) => other is FeatureKeyType key && _group == key._group;

        public override bool Equals(object? other) => other is FeatureKeyType key && _group == key._group;

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_group);
    }

    private sealed class SequenceFeatures(KeyValuePair<Type, object>[] values) : IFeatureCollection
    {
        public bool IsReadOnly => false;

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
            => ((IEnumerable<KeyValuePair<Type, object>>)values).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
