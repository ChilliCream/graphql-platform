using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class FragmentsOnCompositeTypesRuleTests
    : DocumentValidatorVisitorTestBase
{
    public FragmentsOnCompositeTypesRuleTests()
        : base(builder => builder.AddFragmentRules())
    {
    }

    /// <summary>
    /// Validate: Fragments on composite types
    /// </summary>
    [Fact]
    public void Fragment_On_Object_Is_Valid()
    {
        ExpectValid(@"
                {
                    dog {
                       ... fragOnObject
                    }
                }

                fragment fragOnObject on Dog {
                    name
                }
            ");
    }

    /// <summary>
    /// Interface is valid fragment type
    /// </summary>
    [Fact]
    public void Fragment_On_Interface_Is_Valid()
    {
        ExpectValid(@"
                {
                    dog {
                       ... fragOnInterface
                    }
                }

                fragment fragOnInterface on Pet {
                    name
                }
            ");
    }

    /// <summary>
    /// Object is valid inline fragment type
    /// </summary>
    [Fact]
    public void Object_Is_Valid_Inline_FragmentType()
    {
        ExpectValid(@"
                {
                    dog {
                       ... validFragment
                    }
                }

                fragment validFragment on Pet {
                    ... on Dog {
                        barkVolume
                    }
                }
            ");
    }

    /// <summary>
    /// interface is valid inline fragment type
    /// </summary>
    [Fact]
    public void Interface_Is_Valid_Inline_FragmentType()
    {
        ExpectValid(@"
                {
                    dog {
                       ... validFragment
                    }
                }

                fragment validFragment on Mammal {
                    ... on Canine {
                        name
                    }
                }
            ");
    }

    /// <summary>
    /// inline fragment without type is valid
    /// </summary>
    [Fact]
    public void InlineFragment_Without_Type_Is_Valid()
    {
        ExpectValid(@"
                {
                    dog {
                       ... validFragment
                    }
                }

                fragment validFragment on Pet {
                    ... {
                        name
                    }
                }
            ");
    }

    /// <summary>
    /// union is valid fragment type
    /// </summary>
    [Fact]
    public void Fragment_On_Union_Is_Valid()
    {
        // arrange
        ExpectValid(@"
                {
                    dog {
                       ... fragOnUnion
                    }
                }

                fragment fragOnUnion on CatOrDog {
                    ... on Dog {
                        name
                    }
                }
            ");
    }

    [Fact]
    public void Fragment_On_Scalar_Is_Invalid()
    {
        ExpectErrors(@"
                {
                    dog {
                       ... fragOnScalar
                    }
                }

                fragment fragOnScalar on Int {
                    something
                }
            ",
            t => Assert.Equal(
                "Fragments can only be declared on unions, interfaces, and objects.",
                t.Message));
    }

    [Fact]
    public void InlineFragment_On_Scalar_Is_Invalid()
    {
        ExpectErrors(@"
                {
                    dog {
                       ... inlineFragOnScalar
                    }
                }

                fragment inlineFragOnScalar on Dog {
                    ... on Boolean {
                        somethingElse
                    }
                }
            ",
            t => Assert.Equal(
                "Fragments can only be declared on unions, interfaces, and objects.",
                t.Message));
    }
}
