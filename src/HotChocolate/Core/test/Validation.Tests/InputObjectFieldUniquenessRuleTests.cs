using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class InputObjectFieldUniquenessRuleTests : DocumentValidatorVisitorTestBase
{
    public InputObjectFieldUniquenessRuleTests()
        : base(builder => builder.AddValueRules())
    {
    }

    [Fact]
    public void NoFieldAmbiguity()
    {
        ExpectValid(
            @"{
                findDog(complex: { name: ""A"", owner: ""B"" })
            }");
    }

    [Fact]
    public void NameFieldIsAmbiguous()
    {
        // arrange
        ExpectErrors(@"
                {
                    findDog(complex: { name: ""A"", name: ""B"" })
                }
            ",
            error =>
                Assert.Equal("There can be only one input field named `name`.", error.Message));
    }

    [Fact]
    public void InputObjectWithField()
    {
        ExpectValid(@"
                {
                    arguments {
                        complexArgField(complexArg: {requiredField: true, f: true })
                    }
                }
            ");
    }

    [Fact]
    public void SameInputObjectWithinTwoArgs()
    {
        ExpectValid(@"
                {
                    arguments {
                        complexArgField(
                            complexArg1: {requiredField: true, f: true },
                            complexArg2: {requiredField: true, f: true })
                    }
                }
            ");
    }

    [Fact]
    public void MultipleInputObjectFields()
    {
        ExpectValid(@"
                {
                    arguments {
                        complexArgField(complexArg: {
                            requiredField: true,
                            f1: ""value"",
                            f2: ""value"",
                            f3: ""value"" })
                    }
                }
            ");
    }

    [Fact]
    public void AllowsForNestedInputObjectsWithSimilarFields()
    {
        ExpectValid(@"
                {
                    arguments {
                        complexArgField(complexArg: {
                            requiredField: true
                            deep: {
                                requiredField: true
                                deep: {
                                    requiredField: true
                                    id: 1
                                }
                                id: 1
                            }
                            id: 1
                        })
                    }
                }
            ");
    }

    [Fact]
    public void DuplicateInputObjectFields()
    {
        ExpectErrors(@"
                {
                    arguments {
                        complexArgField(complexArg: {
                            requiredField: true,
                            f1: ""value"",
                            f1: ""value"" })
                    }
                }
            ");
    }

    [Fact]
    public void ManyDuplicateInputObjectFields()
    {
        ExpectErrors(@"
                {
                    arguments {
                        complexArgField(complexArg: {
                            requiredField: true,
                            f1: ""value"",
                            f1: ""value"",
                            f1: ""value"" })
                    }
                }
            ");
    }

    [Fact]
    public void NestedDuplicateInputObjectFields()
    {
        ExpectErrors(@"
                {
                    arguments {
                        complexArgField(complexArg: {
                            requiredField: true,
                            deep: {
                                requiredField:true
                                f2: ""value"",
                                f2: ""value"" }})
                    }
                }
            ");
    }
}
