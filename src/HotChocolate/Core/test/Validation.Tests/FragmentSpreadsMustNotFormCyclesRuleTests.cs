using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class FragmentSpreadsMustNotFormCyclesRuleTests
    : DocumentValidatorVisitorTestBase
{
    public FragmentSpreadsMustNotFormCyclesRuleTests()
        : base(builder => builder.AddFragmentRules())
    {
    }

    [Fact]
    public void FragmentCycle1()
    {
        ExpectErrors(@"
                {
                    dog {
                        ...nameFragment
                    }
                }

                fragment nameFragment on Dog {
                    name
                    ...barkVolumeFragment
                }

                fragment barkVolumeFragment on Dog {
                    barkVolume
                    ...nameFragment
                }
            ",
            t => Assert.Equal(
                "The graph of fragment spreads must not form any " +
                "cycles including spreading itself. Otherwise an " +
                "operation could infinitely spread or infinitely " +
                "execute on cycles in the underlying data.",
                t.Message));
    }

    [Fact]
    public void FragmentCycle2()
    {
        ExpectErrors(@"
                {
                    dog {
                        ...nameFragment
                    }
                }

                fragment nameFragment on Dog {
                    name
                    ...barkVolumeFragment
                }

                fragment barkVolumeFragment on Dog {
                    barkVolume
                    ...barkVolumeFragment1
                }

                fragment barkVolumeFragment1 on Dog {
                    barkVolume
                    ...barkVolumeFragment2
                }

                fragment barkVolumeFragment2 on Dog {
                    barkVolume
                    ...nameFragment
                }
            ",
            t => Assert.Equal(
                "The graph of fragment spreads must not form any " +
                "cycles including spreading itself. Otherwise an " +
                "operation could infinitely spread or infinitely " +
                "execute on cycles in the underlying data.",
                t.Message));
    }

    [Fact]
    public void InfiniteRecursion()
    {
        ExpectErrors(@"
                {
                    dog {
                        ...dogFragment
                    }
                }

                fragment dogFragment on Dog {
                    name
                    owner {
                        ...ownerFragment
                    }
                }

                fragment ownerFragment on Human {
                    name
                    pets {
                        ...dogFragment
                    }
                }
            ",
            t => Assert.Equal(
                "The graph of fragment spreads must not form any " +
                "cycles including spreading itself. Otherwise an " +
                "operation could infinitely spread or infinitely " +
                "execute on cycles in the underlying data.",
                t.Message));
    }

    [Fact]
    public void QueryWithSideBySideFragSpreads()
    {
        ExpectValid(@"
                {
                    dog {
                        ...dogFragment
                        ...dogFragment
                        ...dogFragment
                        ...dogFragment
                        ...dogFragment
                        ...dogFragment
                        ...dogFragment
                    }
                }

                fragment dogFragment on Dog {
                    name
                }
            ");
    }

    [Fact]
    public void SingleReferenceIsValid()
    {
        ExpectValid(@"
                {
                    dog {
                        ...fragA
                    }
                }

                fragment fragA on Dog { ...fragB }
                fragment fragB on Dog { name }
            ");
    }

    [Fact]
    public void SpreadTwiceIsNotCircular()
    {
        ExpectValid(@"
                {
                    dog {
                        ...fragA
                    }
                }

                fragment fragA on Dog { ...fragB, ...fragB }
                fragment fragB on Dog { name }
            ");
    }

    [Fact]
    public void SpreadTwiceIndirectlyIsNotCircular()
    {
        ExpectValid(@"
                {
                    dog {
                        ...fragA
                    }
                }

                fragment fragA on Dog { ...fragB, ...fragC }
                fragment fragB on Dog { ...fragC }
                fragment fragC on Dog { name }
            ");
    }

    [Fact]
    public void DoubleSpreadWithinAbstractTypes()
    {
        ExpectValid(@"
                {
                    human {
                        pets {
                            ...spreadsInAnon
                        }
                    }
                }

                fragment nameFragment on Pet {
                    ... on Dog { name }
                    ... on Cat { name }
                }

                fragment spreadsInAnon on Pet {
                    ... on Dog { ...nameFragment }
                    ... on Cat { ...nameFragment }
                }
            ");
    }

    [Fact]
    public void SpeardingRecursivelyWithinFieldFails()
    {
        ExpectErrors(@"
                {
                    human {
                        ...fragA
                    }
                }

                fragment fragA on Human { relatives { ...fragA } },
            ");
    }

    [Fact]
    public void NoSpreadingItselfDirectly()
    {
        ExpectErrors(@"
                {
                    dog {
                        ...fragA
                    }
                }

                fragment fragA on Dog { ...fragA }
            ");
    }

    [Fact]
    public void NoSpreadingItselfDirectlyWithinInlineFragment()
    {
        ExpectErrors(@"
                {
                    human {
                        pets {
                            ...fragA
                        }
                    }
                }

                fragment fragA on Pet {
                    ... on Dog {
                    ...fragA
                    }
                }
            ");
    }

    [Fact]
    public void NoSpreadingItselfIndirectly()
    {
        ExpectErrors(@"
                {
                    dog {
                        ...fragA
                    }
                }

                fragment fragA on Dog { ...fragB }
                fragment fragB on Dog { ...fragA }
            ");
    }

    [Fact]
    public void NoSpreadingItselfIndirectlyWithinInlineFragment()
    {
        ExpectErrors(@"
                {
                    human {
                        pets {
                            ...fragA
                        }
                    }
                }

                fragment fragA on Pet {
                    ... on Dog {
                        ...fragB
                    }
                }

                fragment fragB on Pet {
                    ... on Dog {
                        ...fragA
                    }
                }
            ");
    }

    [Fact]
    public void NoSpreadingItselfDeeply()
    {
        ExpectErrors(@"
                {
                    dog {
                        ...fragA
                    }
                }

                fragment fragA on Dog { ...fragB }
                fragment fragB on Dog { ...fragC }
                fragment fragC on Dog { ...fragO }
                fragment fragX on Dog { ...fragY }
                fragment fragY on Dog { ...fragZ }
                fragment fragZ on Dog { ...fragO }
                fragment fragO on Dog { ...fragP }
                fragment fragP on Dog { ...fragA, ...fragX }
            ");
    }

    [Fact]
    public void NoSpreadingItselfDeeplyTwoPaths()
    {
        ExpectErrors(@"
                {
                    dog {
                        ...fragA
                    }
                }

                fragment fragA on Dog { ...fragB, ...fragC }
                fragment fragB on Dog { ...fragA }
                fragment fragC on Dog { ...fragA }
            ");
    }

    [Fact]
    public void NoSpreadingItselfDeeplyTwoPathsAltTraverseOrder()
    {
        ExpectErrors(@"
                {
                    dog {
                        ...fragA
                    }
                }

                fragment fragA on Dog { ...fragC }
                fragment fragB on Dog { ...fragC }
                fragment fragC on Dog { ...fragA, ...fragB }
            ");
    }

    [Fact]
    public void NoSpreadingItselfDeeplyAndImmediately()
    {
        ExpectErrors(@"
                {
                    dog {
                        ...fragA
                    }
                }

                fragment fragA on Dog { ...fragB }
                fragment fragB on Dog { ...fragB, ...fragC }
                fragment fragC on Dog { ...fragA, ...fragB }
            ");
    }

    [Fact]
    public void DoesNotInfiniteLoopOnRecursiveFragment()
    {
        ExpectErrors(@"
                {
                    dogOrHuman {
                        ... fragA
                    }
                }

                fragment fragA on Human {
                    name
                    ... fragA
                }");
    }

    [Fact]
    public void DoesNotInfiniteLoopOnImmediatelyRecursiveFragment()
    {
        ExpectErrors(@"
                {
                    dogOrHuman {
                        ... fragA
                    }
                }

                fragment fragA on Human {
                    name
                    relatives {
                        name
                        ... fragA
                    }
                }");
    }

    [Fact]
    public void DoesNotInfiniteLoopOnTransitivelyRecursiveFragment()
    {
        ExpectErrors(@"
                {
                    dogOrHuman {
                        ... fragA
                        ... fragB
                        ... fragC
                    }
                }

                fragment fragA on Human { name, ...fragB }
                fragment fragB on Human { name, ...fragC }
                fragment fragC on Human { name, ...fragA }");
    }
}
