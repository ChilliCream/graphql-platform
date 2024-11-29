using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class AllVariablesUsedRuleTests
    : DocumentValidatorVisitorTestBase
{
    public AllVariablesUsedRuleTests()
        : base(builder => builder.AddVariableRules())
    {
    }

    [Fact]
    public void VariableUnused()
    {
        // arrange
        ExpectErrors(@"
                query variableUnused($atOtherHomes: Boolean) {
                    dog {
                        isHouseTrained
                    }
                }
            ",
            t => Assert.Equal(
                "The following variables were not used: " +
                "atOtherHomes.", t.Message));
    }

    [Fact]
    public void VariableUsedInFragment()
    {
        ExpectValid(@"
                query variableUsedInFragment($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                fragment isHousetrainedFragment on Dog {
                    isHouseTrained(atOtherHomes: $atOtherHomes)
                }
            ");
    }

    [Fact]
    public void VariableUsedInSecondLevelFragment()
    {
        ExpectValid(@"
                query variableUsedInFragment($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                fragment isHousetrainedFragment on Dog {
                    ...isHousetrainedFragmentLevel2
                }

                fragment isHousetrainedFragmentLevel2 on Dog {
                    isHouseTrained(atOtherHomes: $atOtherHomes)
                }
            ");
    }

    [Fact]
    public void VariableUsedInDirective()
    {
        ExpectValid(@"
                query variableUsedInFragment($atOtherHomes: Boolean!) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                fragment isHousetrainedFragment on Dog {
                    isHouseTrained @skip(if: $atOtherHomes)
                }
            ");

        ExpectValid(@"
                query variableUsedInFragment($atOtherHomes: Boolean!) {
                    dog {
                        ...isHousetrainedFragment @skip(if: $atOtherHomes)
                    }
                }

                fragment isHousetrainedFragment on Dog {
                    isHouseTrained
                }
            ");
    }

    [Fact]
    public void VariableNotUsedWithinFragment()
    {
        // arrange
        ExpectErrors(@"
                query variableNotUsedWithinFragment($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedWithoutVariableFragment
                    }
                }

                fragment isHousetrainedWithoutVariableFragment on Dog {
                    isHouseTrained
                }
            ",
            t => Assert.Equal(
                "The following variables were not used: " +
                "atOtherHomes.", t.Message));
    }

    [Fact]
    public void QueryWithExtraVar()
    {
        ExpectErrors(@"
                query queryWithUsedVar($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                query queryWithExtraVar($atOtherHomes: Boolean, $extra: Int) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                fragment isHousetrainedFragment on Dog {
                    isHouseTrained(atOtherHomes: $atOtherHomes)
                }
            ",
            t => Assert.Equal(
                "The following variables were not used: " +
                "extra.", t.Message));
    }

    [Fact]
    public void VariableUsedAndDeclared()
    {
        ExpectValid(@"
                query variableIsDefined($atOtherHomes: Boolean)
                {
                    dog {
                        isHouseTrained(atOtherHomes: $atOtherHomes)
                    }
                }");
    }

    [Fact]
    public void VariableUsedInComplexInput()
    {
        ExpectValid(@"
                query queryWithComplexInput($name: String)
                {
                    findDog(complex: { name: $name }) {
                        name
                    }
                }");
    }

    [Fact]
    public void VariableUsedInListInput()
    {
        ExpectValid(@"
                query queryWithListInput($value: Boolean!)
                {
                    booleanList(booleanListArg: [ $value ])
                }");
    }

    [Fact]
    public void VariableUsedAndNotDeclared()
    {
        // arrange
        ExpectErrors(@"
                query variableIsDefined
                {
                    dog {
                        isHouseTrained(atOtherHomes: $atOtherHomes)
                    }
                }",
            t => Assert.Equal(
                "The following variables were not declared: " +
                "atOtherHomes.", t.Message));
    }

    [Fact]
    public void VariableUsedAndNotDeclared2()
    {
        ExpectErrors(@"
                query variableIsNotDefinedUsedInNestedFragment {
                    dog {
                        ...outerHousetrainedFragment
                    }
                }

                fragment outerHousetrainedFragment on Dog {
                    ...isHousetrainedFragment
                }

                fragment isHousetrainedFragment on Dog {
                    isHouseTrained(atOtherHomes: $atOtherHomes)
                }",
            t => Assert.Equal(
                "The following variables were not declared: " +
                "atOtherHomes.", t.Message));
    }

    [Fact]
    public void VarsMustBeDefinedInAllOperationsInWhichAFragmentIsUsed()
    {
        ExpectValid(@"
                query housetrainedQueryOne($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                query housetrainedQueryTwo($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                query housetrainedQueryThree {
                    dog {
                        isHouseTrained(atOtherHomes: true)
                    }
                }

                fragment isHousetrainedFragment on Dog {
                    isHouseTrained(atOtherHomes: $atOtherHomes)
                }");
    }

    [Fact]
    public void VarsMustBeDefinedInAllOperationsInWhichAFragmentIsUsedErr()
    {
        ExpectErrors(@"
                query variableIsNotDefinedUsedInNestedFragment {
                    dog {
                        ...outerHousetrainedFragment
                    }
                }

                fragment outerHousetrainedFragment on Dog {
                    ...isHousetrainedFragment
                }

                fragment isHousetrainedFragment on Dog {
                    isHouseTrained(atOtherHomes: $atOtherHomes)
                }",
            t => Assert.Equal(
                "The following variables were not declared: " +
                "atOtherHomes.", t.Message));
    }
}
