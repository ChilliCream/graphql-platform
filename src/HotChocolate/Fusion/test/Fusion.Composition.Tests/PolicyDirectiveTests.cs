using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Directives;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using Directive = HotChocolate.Types.Mutable.Directive;

namespace HotChocolate.Fusion;

public sealed class PolicyDirectiveTests
{
    [Fact]
    public void From_Should_CoerceStringToSingleNameGroup_When_NamesIsString()
    {
        // arrange
        var directive = CreateDirective(new StringValueNode("CanRead"));

        // act
        var policy = PolicyDirective.From(directive);

        // assert
        Assert.Equal("CanRead", Assert.Single(Assert.Single(policy.Groups)));
    }

    [Fact]
    public void From_Should_CoerceEachItemToGroup_When_NamesIsFlatList()
    {
        // arrange
        var directive = CreateDirective(
            new ListValueNode(new StringValueNode("CanB"), new StringValueNode("CanA")));

        // act
        var policy = PolicyDirective.From(directive);

        // assert
        Assert.Collection(
            policy.Groups,
            group => Assert.Equal("CanA", Assert.Single(group)),
            group => Assert.Equal("CanB", Assert.Single(group)));
    }

    [Fact]
    public void From_Should_ParseGroups_When_NamesIsNestedList()
    {
        // arrange
        var directive = CreateDirective(
            new ListValueNode(
                new ListValueNode(new StringValueNode("b"), new StringValueNode("a")),
                new ListValueNode(new StringValueNode("c"))));

        // act
        var policy = PolicyDirective.From(directive);

        // assert
        Assert.Collection(
            policy.Groups,
            group => Assert.Equal(new[] { "a", "b" }, group),
            group => Assert.Equal("c", Assert.Single(group)));
    }

    [Fact]
    public void From_Should_CoerceMixedItems_When_NamesMixesStringsAndLists()
    {
        // arrange
        var directive = CreateDirective(
            new ListValueNode(
                new StringValueNode("CanAdmin"),
                new ListValueNode(new StringValueNode("CanRead"), new StringValueNode("CanAudit"))));

        // act
        var policy = PolicyDirective.From(directive);

        // assert
        Assert.Collection(
            policy.Groups,
            group => Assert.Equal("CanAdmin", Assert.Single(group)),
            group => Assert.Equal(new[] { "CanAudit", "CanRead" }, group));
    }

    [Fact]
    public void From_Should_CanonicalizeExpression_When_NamesContainDuplicates()
    {
        // arrange
        var directive = CreateDirective(
            new ListValueNode(
                new ListValueNode(new StringValueNode("a"), new StringValueNode("a")),
                new ListValueNode(new StringValueNode("a"))));

        // act
        var policy = PolicyDirective.From(directive);

        // assert
        Assert.Equal("a", Assert.Single(Assert.Single(policy.Groups)));
    }

    [Fact]
    public void From_Should_ShareCanonicalKey_When_ExpressionsDifferOnlyInOrder()
    {
        // arrange
        var first = CreateDirective(
            new ListValueNode(
                new ListValueNode(new StringValueNode("a"), new StringValueNode("b")),
                new StringValueNode("c")));
        var second = CreateDirective(
            new ListValueNode(
                new StringValueNode("c"),
                new ListValueNode(new StringValueNode("b"), new StringValueNode("a"))));

        // act
        var firstPolicy = PolicyDirective.From(first);
        var secondPolicy = PolicyDirective.From(second);

        // assert
        Assert.Equal(firstPolicy.CanonicalKey, secondPolicy.CanonicalKey);
    }

    [Fact]
    public void From_Should_CreateDistinctCanonicalKey_When_NameContainsNameSeparator()
    {
        // arrange
        // "a\u001fb" is one policy name that contains the character used to
        // join names within a canonical key group.
        var single = CreateDirective(new StringValueNode("a\u001fb"));
        var andGroup = CreateDirective(
            new ListValueNode(
                new ListValueNode(new StringValueNode("a"), new StringValueNode("b"))));

        // act
        var singlePolicy = PolicyDirective.From(single);
        var andPolicy = PolicyDirective.From(andGroup);

        // assert
        Assert.NotEqual(singlePolicy.CanonicalKey, andPolicy.CanonicalKey);
    }

    [Fact]
    public void From_Should_CreateDistinctCanonicalKey_When_NameContainsGroupSeparator()
    {
        // arrange
        // "a\u001eb" is one policy name that contains the character used to
        // join groups within a canonical key.
        var single = CreateDirective(new StringValueNode("a\u001eb"));
        var orGroups = CreateDirective(
            new ListValueNode(new StringValueNode("a"), new StringValueNode("b")));

        // act
        var singlePolicy = PolicyDirective.From(single);
        var orPolicy = PolicyDirective.From(orGroups);

        // assert
        Assert.NotEqual(singlePolicy.CanonicalKey, orPolicy.CanonicalKey);
    }

    [Fact]
    public void From_Should_Throw_When_NamesIsMissing()
    {
        // arrange
        var directive = new Directive(s_policyDirective);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PolicyDirective.From(directive));

        // assert
        Assert.Equal(
            "The `names` argument is required on the @policy directive.",
            exception.Message);
    }

    [Fact]
    public void From_Should_Throw_When_OuterListIsEmpty()
    {
        // arrange
        var directive = CreateDirective(new ListValueNode());

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PolicyDirective.From(directive));

        // assert
        Assert.Equal(
            "The `names` argument on @policy must contain at least one policy name group.",
            exception.Message);
    }

    [Fact]
    public void From_Should_Throw_When_GroupIsEmpty()
    {
        // arrange
        var directive = CreateDirective(new ListValueNode(new ListValueNode()));

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PolicyDirective.From(directive));

        // assert
        Assert.Equal(
            "A policy name group on @policy must contain at least one policy name.",
            exception.Message);
    }

    [Fact]
    public void From_Should_Throw_When_NameInGroupIsNull()
    {
        // arrange
        var directive = CreateDirective(
            new ListValueNode(new ListValueNode(NullValueNode.Default)));

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PolicyDirective.From(directive));

        // assert
        Assert.Equal("A policy name on @policy must be a string.", exception.Message);
    }

    [Fact]
    public void From_Should_Throw_When_GroupItemIsNull()
    {
        // arrange
        var directive = CreateDirective(new ListValueNode(NullValueNode.Default));

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PolicyDirective.From(directive));

        // assert
        Assert.Equal(
            "A policy name group on @policy must be a string or a list of strings.",
            exception.Message);
    }

    [Fact]
    public void From_Should_Throw_When_NameIsNotString()
    {
        // arrange
        var directive = CreateDirective(
            new ListValueNode(new ListValueNode(new IntValueNode(1))));

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PolicyDirective.From(directive));

        // assert
        Assert.Equal("A policy name on @policy must be a string.", exception.Message);
    }

    [Fact]
    public void From_Should_Throw_When_NamesIsNotStringOrList()
    {
        // arrange
        var directive = CreateDirective(new IntValueNode(1));

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PolicyDirective.From(directive));

        // assert
        Assert.Equal(
            "The `names` argument on @policy must be a string or a list of policy name groups.",
            exception.Message);
    }

    private static readonly PolicyDenialBehaviorMutableEnumTypeDefinition s_policyDenialBehaviorEnum = new();

    private static readonly PolicyMutableDirectiveDefinition s_policyDirective
        = new(BuiltIns.String.Create(), s_policyDenialBehaviorEnum);

    private static Directive CreateDirective(IValueNode names)
        => new(
            s_policyDirective,
            new ArgumentAssignment(WellKnownArgumentNames.Names, names));
}
