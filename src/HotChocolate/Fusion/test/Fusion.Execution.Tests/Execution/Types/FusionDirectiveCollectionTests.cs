using System.Collections;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionDirectiveCollectionTests
{
    [Fact]
    public void Constructor_Should_StablyPartitionDirectives_When_InternalsAreInterleaved()
    {
        // arrange
        var publicOne = Directive("publicOne");
        var internalOne = Directive("internalOne", isPublic: false);
        var publicTwo = Directive("publicTwo");
        var internalTwo = Directive("internalTwo", isPublic: false);
        var publicThree = Directive("publicThree");

        // act
        var collection = new FusionDirectiveCollection(
            [publicOne, internalOne, publicTwo, internalTwo, publicThree]);

        // assert
        Assert.Equal(3, collection.Count);
        Assert.Equal(5, collection.WithInternals.Count);
        AssertDirectives(
            collection,
            publicOne,
            publicTwo,
            publicThree);
        AssertDirectives(
            collection.WithInternals.AsEnumerable(),
            publicOne,
            publicTwo,
            publicThree,
            internalOne,
            internalTwo);
    }

    [Fact]
    public void PublicCollection_Should_IgnoreInternalDirectives_When_UsingConcreteApi()
    {
        // arrange
        var publicShared = Directive("shared");
        var internalShared = Directive("shared", isPublic: false);
        var publicOnly = Directive("publicOnly");
        var internalOnly = Directive("internalOnly", isPublic: false);
        var collection = new FusionDirectiveCollection(
            [internalShared, publicShared, internalOnly, publicOnly]);
        var copy = new FusionDirective[collection.Count];

        // act
        collection.CopyTo(copy, 0);

        // assert
        Assert.Equal(2, collection.Count);
        Assert.Same(publicShared, collection[0]);
        Assert.Same(publicOnly, collection[1]);
        AssertDirectives(collection["shared"], publicShared);
        Assert.Same(publicShared, collection.FirstOrDefault("shared"));
        Assert.Null(collection.FirstOrDefault("internalOnly"));
        Assert.True(collection.ContainsName("shared"));
        Assert.False(collection.ContainsName("internalOnly"));
        Assert.True(collection.Contains(publicShared));
        Assert.False(collection.Contains(internalShared));
        Assert.False(collection.Contains(internalOnly));
        AssertDirectives(copy, publicShared, publicOnly);
        AssertDirectives(collection.AsEnumerable(), publicShared, publicOnly);
        AssertDirectives(collection, publicShared, publicOnly);
        Assert.Equal(
            ["shared", "publicOnly"],
            collection.ToSyntaxNodes().Select(t => t.Name.Value));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection[2]);
    }

    [Fact]
    public void PublicCollection_Should_IgnoreInternalDirectives_When_UsingInterfaceApi()
    {
        // arrange
        var publicOne = Directive("publicOne");
        var internalOne = Directive("internalOne", isPublic: false);
        var publicTwo = Directive("publicTwo");
        var collection = new FusionDirectiveCollection([internalOne, publicOne, publicTwo]);
        IReadOnlyDirectiveCollection directives = collection;
        IReadOnlyList<FusionDirective> fusionDirectives = collection;
        IEnumerable<IDirective> directiveEnumerable = collection;
        IEnumerable<FusionDirective> fusionDirectiveEnumerable = collection;
        IEnumerable nonGenericEnumerable = collection;

        // assert
        Assert.Equal(2, directives.Count);
        Assert.Equal(2, fusionDirectives.Count);
        Assert.Same(publicOne, directives[0]);
        Assert.Same(publicTwo, fusionDirectives[1]);
        AssertDirectives(directives["publicOne"], publicOne);
        Assert.Empty(directives["internalOne"]);
        Assert.Same(publicOne, directives.FirstOrDefault("publicOne"));
        Assert.Null(directives.FirstOrDefault("internalOne"));
        Assert.Same(publicOne, directives.FirstOrDefault(typeof(object)));
        Assert.True(directives.ContainsName("publicOne"));
        Assert.False(directives.ContainsName("internalOne"));
        AssertDirectives(directiveEnumerable, publicOne, publicTwo);
        AssertDirectives(fusionDirectiveEnumerable, publicOne, publicTwo);
        AssertDirectives(
            nonGenericEnumerable.Cast<FusionDirective>(),
            publicOne,
            publicTwo);
        Assert.Throws<ArgumentOutOfRangeException>(() => directives[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => fusionDirectives[2]);

        IReadOnlyDirectiveCollection internalOnlyCollection =
            new FusionDirectiveCollection([internalOne]);
        Assert.Null(internalOnlyCollection.FirstOrDefault(typeof(object)));
    }

    [Fact]
    public void WithInternals_Should_IncludeAllDirectives_When_UsingConcreteApi()
    {
        // arrange
        var publicOne = Directive("shared");
        var internalOne = Directive("shared", isPublic: false);
        var publicTwo = Directive("publicTwo");
        var internalTwo = Directive("internalTwo", isPublic: false);
        var collection = new FusionDirectiveCollection(
            [internalOne, publicOne, internalTwo, publicTwo]);
        var directives = collection.WithInternals;
        var copy = new FusionDirective[directives.Count];
        var enumerated = new List<FusionDirective>();

        // act
        directives.CopyTo(copy, 0);
        foreach (var directive in directives)
        {
            enumerated.Add(directive);
        }

        // assert
        Assert.Equal(4, directives.Count);
        Assert.Same(publicOne, directives[0]);
        Assert.Same(publicTwo, directives[1]);
        Assert.Same(internalOne, directives[2]);
        Assert.Same(internalTwo, directives[3]);
        AssertDirectives(directives["shared"], publicOne, internalOne);
        Assert.Same(publicOne, directives.FirstOrDefault("shared"));
        Assert.Same(publicOne, directives.FirstOrDefault(typeof(object)));
        Assert.True(directives.ContainsName("shared"));
        Assert.True(directives.Contains(internalOne));
        AssertDirectives(copy, publicOne, publicTwo, internalOne, internalTwo);
        AssertDirectives(
            directives.AsEnumerable(),
            publicOne,
            publicTwo,
            internalOne,
            internalTwo);
        AssertDirectives(
            enumerated,
            publicOne,
            publicTwo,
            internalOne,
            internalTwo);
        Assert.Equal(
            ["shared", "publicTwo", "shared", "internalTwo"],
            directives.ToSyntaxNodes().Select(t => t.Name.Value));
        Assert.Throws<ArgumentOutOfRangeException>(() => directives[4]);

        var internalOnly = new FusionDirectiveCollection([internalOne]).WithInternals;
        Assert.Same(internalOne, internalOnly.FirstOrDefault(typeof(object)));
    }

    [Fact]
    public void WithInternals_Should_IncludeAllDirectives_When_UsingInterfaceApi()
    {
        // arrange
        var publicOne = Directive("publicOne");
        var internalOne = Directive("internalOne", isPublic: false);
        var collection = new FusionDirectiveCollection([internalOne, publicOne]);
        var view = collection.WithInternals;
        IReadOnlyDirectiveCollection directives = view;
        IReadOnlyList<FusionDirective> fusionDirectives = view;
        IEnumerable<IDirective> directiveEnumerable = view;
        IEnumerable<FusionDirective> fusionDirectiveEnumerable = view;
        IEnumerable nonGenericEnumerable = view;

        // assert
        Assert.Equal(2, directives.Count);
        Assert.Equal(2, fusionDirectives.Count);
        Assert.Same(publicOne, directives[0]);
        Assert.Same(internalOne, fusionDirectives[1]);
        AssertDirectives(directives["internalOne"], internalOne);
        Assert.Same(internalOne, directives.FirstOrDefault("internalOne"));
        Assert.Same(publicOne, directives.FirstOrDefault(typeof(object)));
        Assert.True(directives.ContainsName("internalOne"));
        AssertDirectives(directiveEnumerable, publicOne, internalOne);
        AssertDirectives(fusionDirectiveEnumerable, publicOne, internalOne);
        AssertDirectives(
            nonGenericEnumerable.Cast<FusionDirective>(),
            publicOne,
            internalOne);
        Assert.Throws<ArgumentOutOfRangeException>(() => directives[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => fusionDirectives[2]);
    }

    [Fact]
    public void WithInternalsEnumerator_Should_RejectCurrent_When_EnumeratorIsNotOnElement()
    {
        // arrange
        var directive = Directive("directive");
        var enumerator = new FusionDirectiveCollection([directive])
            .WithInternals
            .GetEnumerator();

        // assert
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Same(directive, enumerator.Current);
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }

    [Fact]
    public void WithInternals_Should_BeEmpty_When_ViewIsDefault()
    {
        // arrange
        FusionDirectiveCollection.AllDirectivesView view = default;
        IReadOnlyDirectiveCollection directives = view;

        // assert
        Assert.Empty(view);
        Assert.Empty(view.AsEnumerable());
        Assert.Empty(view["internal"]);
        Assert.Empty(view.ToSyntaxNodes());
        Assert.Empty(directives);
        Assert.Null(view.FirstOrDefault("internal"));
        Assert.Null(view.FirstOrDefault(typeof(object)));
        Assert.False(view.ContainsName("internal"));
        Assert.Throws<ArgumentOutOfRangeException>(() => view[0]);
    }

    private static FusionDirective Directive(string name)
        => Directive(name, isPublic: true);

    private static FusionDirective Directive(string name, bool isPublic)
        => new(
            new FusionDirectiveDefinition(
                name,
                description: null,
                isDeprecated: false,
                deprecationReason: null,
                isRepeatable: true,
                FusionInputFieldDefinitionCollection.Empty,
                DirectiveLocation.FieldDefinition),
            isPublic);

    private static void AssertDirectives(
        IEnumerable<FusionDirective> actual,
        params FusionDirective[] expected)
        => Assert.Equal(expected, actual);

    private static void AssertDirectives(
        IEnumerable<IDirective> actual,
        params FusionDirective[] expected)
        => Assert.Equal(expected, actual);
}
