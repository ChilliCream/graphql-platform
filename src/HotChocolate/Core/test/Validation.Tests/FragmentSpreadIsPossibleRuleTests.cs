using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class FragmentSpreadIsPossibleRuleTests
    : DocumentValidatorVisitorTestBase
{
    public FragmentSpreadIsPossibleRuleTests()
        : base(builder => builder.AddFragmentRules())
    {
    }

    [Fact]
    public void FragmentDoesNotMatchType()
    {
        ExpectErrors(@"
                {
                    dog {
                        ...fragmentDoesNotMatchType
                    }
                }

                fragment fragmentDoesNotMatchType on Human {
                    name
                }
            ",
            t => Assert.Equal(
                "The parent type does not match the type condition on the fragment.",
                t.Message));
    }

    [Fact]
    public void InterfaceTypeDoesMatch()
    {
        ExpectValid(@"
                {
                    dog {
                        ...interfaceTypeDoesMatch
                    }
                }

                fragment interfaceTypeDoesMatch on Pet {
                    name
                }
            ");
    }

    [Fact]
    public void UnionTypeDoesMatch()
    {
        ExpectValid(@"
                {
                    dog {
                        ...unionTypeDoesMatch
                    }
                }

                fragment unionTypeDoesMatch on CatOrDog {
                    __typename
                }
            ");
    }

    [Fact]
    public void ObjectTypeDoesMatch()
    {
        ExpectValid(@"
                {
                    dog {
                        ...objectTypeDoesMatch
                    }
                }

                fragment objectTypeDoesMatch on Dog {
                    name
                }
            ");
    }

    [Fact]
    public void Star_Wars_With_Inline_Fragments()
    {
        ExpectValid(
            StarWars,
            @"
                query ExecutionDepthShouldNotLeadToEmptyObects {
                    hero(episode: NEW_HOPE) {
                        __typename
                        id
                        name
                        ... on Human {
                            __typename
                            homePlanet
                        }
                        ... on Droid {
                            __typename
                            primaryFunction
                        }
                        friends {
                            nodes {
                                __typename
                                ... on Human {
                                    __typename
                                    homePlanet
                                    friends {
                                        __typename
                                    }
                                }
                                ... on Droid {
                                    __typename
                                    primaryFunction
                                    friends {
                                        __typename
                                    }
                                }
                            }
                        }
                    }
                }");
    }

    [Fact]
    public void OfTheSameObject()
    {
        ExpectValid(@"
                {
                    dog {
                        ...objectWithinObject
                    }
                }

                fragment objectWithinObject on Dog { ...dogFragment }
                fragment dogFragment on Dog { barkVolume }
            ");
    }

    [Fact]
    public void OfTheSameObjectWithInlineFragment()
    {
        ExpectValid(@"
                {
                    dog {
                        ...objectWithinObjectAnon
                    }
                }

                fragment objectWithinObjectAnon on Dog { ... on Dog { barkVolume } }
            ");
    }

    [Fact]
    public void ObjectIntoAnImplementedInterface()
    {
        ExpectValid(@"
                {
                    human{
                        pets {
                        ...objectWithinInterface
                        }
                    }
                }

                fragment objectWithinInterface on Pet { ...dogFragment }
                fragment dogFragment on Dog { barkVolume }
            ");
    }

    [Fact]
    public void ObjectIntoContainingUnion()
    {
        ExpectValid(@"
                {
                    catOrDog {
                        ...objectWithinUnion
                    }
                }

                fragment objectWithinUnion on CatOrDog { ...dogFragment }
                fragment dogFragment on Dog { barkVolume }
            ");
    }

    [Fact]
    public void UnionIntoContainedObject()
    {
        ExpectValid(@"
                {
                    dog {
                        ...unionWithinObject
                    }
                }

                fragment unionWithinObject on Dog { ...catOrDogFragment }
                fragment catOrDogFragment on CatOrDog { __typename }
            ");
    }

    [Fact]
    public void UnionIntoOverlappingInterface()
    {
        ExpectValid(@"
                {
                    human{
                        pets {
                        ...unionWithinInterface
                        }
                    }
                }

                fragment unionWithinInterface on Pet { ...catOrDogFragment }
                fragment catOrDogFragment on CatOrDog { __typename }
            ");
    }

    [Fact]
    public void UnionIntoOverlappingUnion()
    {
        ExpectValid(@"
                {
                    dogOrHuman {
                        ...unionWithinUnion
                    }
                }

                fragment unionWithinUnion on DogOrHuman { ...catOrDogFragment }
                fragment catOrDogFragment on CatOrDog { __typename }
            ");
    }

    [Fact]
    public void InterfaceIntoImplementedObject()
    {
        ExpectValid(@"
                {
                    dog {
                        ...interfaceWithinObject
                    }
                }

                fragment interfaceWithinObject on Dog { ...petFragment }
                fragment petFragment on Pet { name }
            ");
    }

    [Fact]
    public void InterfaceIntoOverlappingInterface()
    {
        ExpectValid(@"
                {
                    human{
                        pets {
                            ...interfaceWithinInterface
                        }
                    }
                }

                fragment interfaceWithinInterface on Pet { ...beingFragment }
                fragment beingFragment on Being { name }
            ");
    }

    [Fact]
    public void InterfaceIntoOverlappingInterfaceInInlineFragment()
    {
        ExpectValid(@"
                {
                    human{
                        pets {
                            ...interfaceWithinInterface
                        }
                    }
                }

                fragment interfaceWithinInterface on Pet { ... on Being { name } }
            ");
    }

    [Fact]
    public void InterfaceIntoOverlappingUnion()
    {
        ExpectValid(@"
                {
                    catOrDog {
                        ...objectWithinUnion
                    }
                }

                fragment objectWithinUnion on CatOrDog { ...dogFragment }
                fragment dogFragment on Dog { barkVolume }
            ");
    }

    [Fact]
    public void DifferentObjectIntoObject()
    {
        ExpectErrors(@"
                {
                    human{
                        pets {
                            ...invalidObjectWithinObject
                        }
                    }
                }

                fragment invalidObjectWithinObject on Cat { ...dogFragment }
                fragment dogFragment on Dog { barkVolume }
            ");
    }

    [Fact]
    public void DifferentObjectIntoObjectInInlineFragment()
    {
        ExpectErrors(@"
                {
                    human{
                        pets {
                            ...invalidObjectWithinObjectAnon
                        }
                    }
                }

                fragment invalidObjectWithinObjectAnon on Cat {
                    ... on Dog { barkVolume }
                }
            ");
    }

    [Fact]
    public void ObjectIntoNotImplementingInterface()
    {
        ExpectErrors(@"
                {
                    human{
                        pets {
                            ...invalidObjectWithinInterface
                        }
                    }
                }

                fragment invalidObjectWithinInterface on Pet { ...humanFragment }
                fragment humanFragment on Human { pets { name } }
            ");
    }

    [Fact]
    public void ObjectIntoNotContainingUnion()
    {
        ExpectErrors(@"
                {
                    catOrDog {
                        ...invalidObjectWithinUnion
                    }
                }

                fragment invalidObjectWithinUnion on CatOrDog { ...humanFragment }
                fragment humanFragment on Human { pets { name } }
            ");
    }

    [Fact]
    public void UnionIntoNotContainedObject()
    {
        ExpectErrors(@"
                {
                    human {
                        ...invalidUnionWithinObject
                    }
                }

                fragment invalidUnionWithinObject on Human { ...catOrDogFragment }
                fragment catOrDogFragment on CatOrDog { __typename }
            ");
    }

    [Fact]
    public void UnionIntoNonOverlappingInterface()
    {
        ExpectErrors(@"
                {
                    human{
                        pets {
                            ...invalidUnionWithinInterface
                        }
                    }
                }

                fragment invalidUnionWithinInterface on Pet { ...humanOrAlienFragment }
                fragment humanOrAlienFragment on HumanOrAlien { __typename }
            ");
    }

    [Fact]
    public void UnionIntoNonOverlappingUnion()
    {
        ExpectErrors(@"
                {
                    catOrDog {
                        ...invalidUnionWithinUnion
                    }
                }

                fragment invalidUnionWithinUnion on CatOrDog { ...humanOrAlienFragment }
                fragment humanOrAlienFragment on HumanOrAlien { __typename }
            ");
    }

    [Fact]
    public void InterfaceIntoNonImplementingObject()
    {
        ExpectErrors(@"
                {
                    catOrDog {
                        ...invalidInterfaceWithinObject
                    }
                }

                fragment invalidInterfaceWithinObject on Cat { ...intelligentFragment }
                fragment intelligentFragment on Intelligent { iq }
            ");
    }

    [Fact]
    public void InterfaceIntoNonOverlappingInterface()
    {
        ExpectErrors(@"
                {
                    human{
                        pets {
                            ...invalidInterfaceWithinInterface
                        }
                    }
                }

                fragment invalidInterfaceWithinInterface on Pet {
                    ...intelligentFragment
                }
                fragment intelligentFragment on Intelligent { iq }
            ");
    }

    [Fact]
    public void InterfaceIntoNonOverlappingInterfaceInInlineFragment()
    {
        ExpectErrors(@"
                {
                    human{
                        pets {
                            ...invalidInterfaceWithinInterfaceAnon
                        }
                    }
                }

                fragment invalidInterfaceWithinInterfaceAnon on Pet {
                    ...on Intelligent { iq }
                }
            ");
    }

    [Fact]
    public void InterfaceIntoNonOverlappingUnion()
    {
        ExpectErrors(@"
                {
                    catOrDog {
                        ...invalidInterfaceWithinUnion
                    }
                }

                fragment invalidInterfaceWithinUnion on CatOrDog { ...petFragment }
                fragment petFragment on HumanOrAlien { name }
            ");
    }
}
