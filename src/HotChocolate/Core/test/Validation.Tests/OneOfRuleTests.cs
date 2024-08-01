using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class OneOfRuleTests : DocumentValidatorVisitorTestBase
{
    public OneOfRuleTests()
        : base(builder => builder.AddValueRules())
    {
    }

    [Fact]
    public void EmptyOneOf()
    {
        ExpectErrors(@"
                mutation addPet {
                    addPet(pet: { }) {
                        name
                    }
                }
            ");
    }

    [Fact]
    public void MultipleFieldsAreNotAllowed_1()
    {
        ExpectErrors(@"
                mutation addPet($cat: CatInput, $dog: DogInput) {
                    addPet(pet: {cat: $cat, dog: $dog}) {
                        name
                    }
                }");
    }

    [Fact]
    public void MultipleFieldsAreNotAllowed_2()
    {
        ExpectErrors(@"
                mutation addPet($dog: DogInput) {
                    addPet(pet: { cat: { name: ""Brontie"" }, dog: $dog }) {
                        name
                    }
                }");
    }

    [Fact]
    public void MultipleFieldsAreNotAllowed_3()
    {
        ExpectErrors(@"
                mutation addPet($cat: CatInput, $dog: DogInput) {
                    addPet(pet: {cat: $cat, dog: $dog}) {
                        name
                    }
                }");
    }

    [Fact]
    public void VariablesUsedForOneofInputObjectFieldsMustBeNonNullable_Valid()
    {
        ExpectValid(@"
                mutation addPet($cat: CatInput!) {
                    addPet(pet: { cat: $cat }) {
                        name
                    }
                }");
    }

    [Fact]
    public void VariablesUsedForOneofInputObjectFieldsMustBeNonNullable_Error()
    {
        ExpectErrors(@"
                mutation addPet($cat: CatInput) {
                    addPet(pet: { cat: $cat }) {
                        name
                    }
                }");
    }

    [Fact]
    public void IfFieldWithLiteralValueIsPresentThenTheValueMustNotBeNull_Valid()
    {
        ExpectValid(@"
                mutation addPet {
                    addPet(pet: { cat: { name: ""Brontie"" } }) {
                        name
                    }
                }");
    }

    [Fact]
    public void IfFieldWithLiteralValueIsPresentThenTheValueMustNotBeNull_Error()
    {
        ExpectErrors(@"
                mutation addPet {
                    addPet(pet: { cat: null }) {
                        name
                    }
                }");
    }
}
