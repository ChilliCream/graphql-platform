using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Validation
{
    public class FragmentSpreadIsPossibleRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public FragmentSpreadIsPossibleRuleTests()
            : base(services => services.AddFragmentRules())
        {
        }

        [Fact]
        public void FragmentDoesNotMatchType()
        {
            // arrange
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
            t => Assert.Equal(t.Message,
                    "The parent type does not match the type condition on " +
                    "the fragment."));
        }

        [Fact]
        public void InterfaceTypeDoesMatch()
        {
            // arrange
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
            // arrange
            ExpectValid(@"
                {
                    catOrDog {
                        ...unionTypeDoesMatch
                    }
                }

                fragment unionTypeDoesMatch on CatOrDog {
                    name
                }
            ");
        }

        [Fact]
        public void ObjectTypeDoesMatch()
        {
            // arrange
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
                    hero(episode: NEWHOPE) {
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
}
