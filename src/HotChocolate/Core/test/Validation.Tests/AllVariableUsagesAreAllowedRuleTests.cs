using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class AllVariableUsagesAreAllowedRuleTests
    : DocumentValidatorVisitorTestBase
{
    public AllVariableUsagesAreAllowedRuleTests()
        : base(builder => builder.AddVariableRules())
    {
    }

    [Fact]
    public void IntCannotGoIntoBoolean()
    {
        // arrange
        ExpectErrors(@"
                query intCannotGoIntoBoolean($intArg: Int) {
                    arguments {
                        booleanArgField(booleanArg: $intArg)
                    }
                }
            ",
            t => Assert.Equal(
                "The variable `intArg` is not compatible with the " +
                "type of the current location.",
                t.Message));
    }

    [Fact]
    public void BooleanListCannotGoIntoBoolean()
    {
        // arrange
        ExpectErrors(@"
                query booleanListCannotGoIntoBoolean($booleanListArg: [Boolean]) {
                    arguments {
                        booleanArgField(booleanArg: $booleanListArg)
                    }
                }
            ",
            t => Assert.Equal(
                "The variable `booleanListArg` is not compatible with the " +
                "type of the current location.",
                t.Message));
    }

    [Fact]
    public void BooleanArgQuery()
    {
        // arrange
        ExpectErrors(@"
                query booleanArgQuery($booleanArg: Boolean) {
                    arguments {
                        nonNullBooleanArgField(nonNullBooleanArg: $booleanArg)
                    }
                }
            ",
            t => Assert.Equal(
                "The variable `booleanArg` is not compatible with the " +
                "type of the current location.",
                t.Message));
    }

    [Fact]
    public void NonNullListToList()
    {
        // arrange
        ExpectValid(@"
                query nonNullListToList($nonNullBooleanList: [Boolean]!) {
                    arguments {
                        booleanListArgField(booleanListArg: $nonNullBooleanList)
                    }
                }
            ");
    }

    [Fact]
    public void BooleanVariableAsListElement()
    {
        // arrange
        ExpectValid(@"
                query nonNullListToList($b: Boolean) {
                    arguments {
                        booleanListArgField(booleanListArg: [$b])
                    }
                }
            ");
    }

    [Fact]
    public void NullableBooleanVariableAsListElement()
    {
        // arrange
        ExpectErrors(@"
                query nonNullBooleanListArgField($nullableBoolean: Boolean) {
                    arguments {
                        nonNullBooleanListArgField(booleanListArg: [$nullableBoolean])
                    }
                }
            ",
            t => Assert.Equal(
                "The variable `nullableBoolean` is not compatible with the " +
                "type of the current location.",
                t.Message));
    }

    [Fact]
    public void ListToNonNullList()
    {
        // arrange
        ExpectErrors(@"
                query listToNonNullList($booleanList: [Boolean]) {
                    arguments {
                        nonNullBooleanListField(nonNullBooleanListArg: $booleanList)
                    }
                }
            ",
            t => Assert.Equal(
                "The variable `booleanList` is not compatible with the " +
                "type of the current location.",
                t.Message));
    }

    [Fact]
    public void BooleanArgQueryWithDefault1()
    {
        ExpectValid(@"
                query booleanArgQueryWithDefault($booleanArg: Boolean) {
                    arguments {
                        optionalNonNullBooleanArgField(optionalBooleanArg: $booleanArg)
                    }
                }
            ");
    }

    [Fact]
    public void BooleanArgQueryWithDefault2()
    {
        ExpectValid(@"
                query booleanArgQueryWithDefault($booleanArg: Boolean = true) {
                    arguments {
                        nonNullBooleanArgField(nonNullBooleanArg: $booleanArg)
                    }
                }
            ");
    }

    [Fact]
    public void BooleanToBoolean()
    {
        ExpectValid(@"
                query Query($booleanArg: Boolean)
                {
                    arguments {
                        booleanArgField(booleanArg: $booleanArg)
                    }
                }
            ");
    }

    [Fact]
    public void BooleanToBooleanWithinFragment()
    {
        ExpectValid(@"
                fragment booleanArgFrag on Arguments {
                    booleanArgField(booleanArg: $booleanArg)
                }

                query Query($booleanArg: Boolean)
                {
                    arguments {
                    ...booleanArgFrag
                    }
                }
            ");
    }

    [Fact]
    public void NonNullableBooleanToBoolean()
    {
        ExpectValid(@"
                query Query($nonNullBooleanArg: Boolean!)
                {
                    arguments {
                        booleanArgField(booleanArg: $nonNullBooleanArg)
                    }
                }
            ");
    }

    [Fact]
    public void NonNullableBooleanToBooleanWithinFragment()
    {
        ExpectValid(@"
                fragment booleanArgFrag on Arguments {
                    booleanArgField(booleanArg: $nonNullBooleanArg)
                }

                query Query($nonNullBooleanArg: Boolean!)
                {
                    arguments {
                        ...booleanArgFrag
                    }
                }
            ");
    }

    [Fact]
    public void StringArrayToStringArray()
    {
        ExpectValid(@"
                query Query($stringListVar: [String])
                {
                    arguments {
                        stringListArgField(stringListArg: $stringListVar)
                    }
                }
            ");
    }

    [Fact]
    public void ElemenIsNonNullableStringArrayToStringArray()
    {
        ExpectValid(@"
                query Query($stringListVar: [String!])
                {
                    arguments {
                        stringListArgField(stringListArg: $stringListVar)
                    }
                }
            ");
    }

    [Fact]
    public void StringToStringInItemPosition()
    {
        ExpectValid(@"
                query Query($stringVar: String)
                {
                    arguments {
                        stringListArgField(stringListArg: [$stringVar])
                    }
                }
            ");
    }

    [Fact]
    public void NonNullableStringToStringInItemPosition()
    {
        ExpectValid(@"
                query Query($stringVar: String!)
                {
                    arguments {
                        stringListArgField(stringListArg: [$stringVar])
                    }
                }
            ");
    }

    [Fact]
    public void ComplexInputToComplexInput()
    {
        ExpectValid(@"
                query Query($complexVar: Complex3Input)
                {
                    arguments {
                        complexArgField(complexArg: $complexVar)
                    }
                }
            ");
    }

    [Fact]
    public void ComplexInputToComplexInputInFieldPosition()
    {
        ExpectValid(@"
                query Query($boolVar: Boolean = false)
                {
                    arguments {
                        complexArgField(complexArg: {requiredField: $boolVar})
                    }
                }
            ");
    }

    [Fact]
    public void NullableBooleanToBooleanInDirective()
    {
        ExpectValid(@"
                query Query($boolVar: Boolean!)
                {
                    dog @include(if: $boolVar)
                }
            ");
    }

    [Fact]
    public void IntToNullableInt()
    {
        ExpectErrors(@"
                query Query($intArg: Int) {
                    arguments {
                        nonNullIntArgField(intArg: $intArg)
                    }
                }
            ");
    }

    [Fact]
    public void IntNullableToIntWithinFragment()
    {
        ExpectErrors(@"
                fragment nonNullIntArgFieldFrag on Arguments {
                    nonNullIntArgField(intArg: $intArg)
                }

                query Query($intArg: Int) {
                    arguments {
                        ...nonNullIntArgFieldFrag
                    }
                }
            ");
    }

    [Fact]
    public void IntNullableToIntWithinNestedFragment()
    {
        ExpectErrors(@"
                fragment outerFrag on Arguments {
                    ...nonNullIntArgFieldFrag
                }

                fragment nonNullIntArgFieldFrag on Arguments {
                    nonNullIntArgField(intArg: $intArg)
                }

                query Query($intArg: Int) {
                    arguments {
                        ...outerFrag
                    }
                }
            ");
    }

    [Fact]
    public void StringOverBoolean()
    {
        ExpectErrors(@"
                query Query($stringVar: String) {
                    arguments {
                        booleanArgField(booleanArg: $stringVar)
                    }
                }
            ");
    }

    [Fact]
    public void StringToStringArray()
    {
        ExpectErrors(@"
                query Query($stringVar: String) {
                    arguments {
                        stringListArgField(stringListArg: $stringVar)
                    }
                }
            ");
    }

    [Fact]
    public void BooleanToBooleanInDirective()
    {
        ExpectErrors(@"
                query Query($boolVar: Boolean) {
                    dog @include(if: $boolVar)
                }
            ");
    }

    [Fact]
    public void StringToNullableBooleanInDirective()
    {
        ExpectErrors(@"
                query Query($stringVar: String) {
                    dog @include(if: $stringVar)
                }
            ");
    }

    [Fact]
    public void StringToElementIsNullableString()
    {
        ExpectErrors(@"
                query Query($stringListVar: [String])
                {
                    arguments {
                        stringListNonNullArgField(stringListNonNullArg: $stringListVar)
                    }
                }
            ");
    }

    [Fact]
    public void IntToNullableIntFailsWhenVariableProvidesNullDefaultValue()
    {
        ExpectErrors(@"
                query Query($intVar: Int = null) {
                    arguments {
                        nonNullIntArgField(intArg: $intVar)
                    }
                }
            ");
    }

    [Fact]
    public void IntToNullableIntWhenVariableProvidesNonNullDefaultValue()
    {
        ExpectValid(@"
                query Query($intVar: Int = 1) {
                    arguments {
                        nonNullIntArgField(intArg: $intVar)
                    }
                }
            ");
    }

    [Fact]
    public void IntToNullableIntWhenOptionalArgumentProvidesDefaultValue()
    {
        ExpectValid(@"
                query Query($intVar: Int) {
                    arguments {
                        nonNullFieldWithDefault(nonNullIntArg: $intVar)
                    }
                }
            ");
    }

    [Fact]
    public void BooleanToNullableBooleanInDirectiveWithDefaultValueWithOption()
    {
        ExpectValid(@"
                query Query($boolVar: Boolean = false) {
                    dog @include(if: $boolVar)
                }
            ");
    }
}
