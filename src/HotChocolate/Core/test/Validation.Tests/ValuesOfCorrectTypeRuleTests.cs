using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Validation
{
    public class ValuesOfCorrectTypeRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public ValuesOfCorrectTypeRuleTests()
            : base(builder => builder.AddValueRules())
        {
        }

        [Fact]
        public void GoodBooleanArg()
        {
            ExpectValid(@"
                {
                    arguments {
                        ...goodBooleanArg
                    }
                }

                fragment goodBooleanArg on Arguments {
                    booleanArgField(booleanArg: true)
                }
            ");
        }

        [Fact]
        public void GoodBooleanListArg()
        {
            ExpectValid(@"
                query queryWithListInput()
                {
                    booleanList(booleanListArg: [ true, false ])
                }");
        }

        [Fact]
        public void GoodBooleanListVariableArg()
        {
            ExpectValid(@"
                query queryWithListInput($value: Boolean!)
                {
                    booleanList(booleanListArg: [ true, $value ])
                }");
        }

        [Fact]
        public void BadBooleanListArg()
        {
            ExpectErrors(@"
                query queryWithListInput()
                {
                    booleanList(booleanListArg: [ true, ""false"" ])
                }",
                 t =>
                 {
                     Assert.Equal(
                         "The specified argument value does not" +
                         " match the argument type.",
                         t.Message);
                     Assert.Equal("[Boolean!]", t.Extensions!["locationType"]);
                     Assert.Equal("booleanListArg", t.Extensions["argument"]);
                 });
        }

        [Fact]
        public void BadBooleanListArgString()
        {
            ExpectErrors(@"
                query queryWithListInput()
                {
                    booleanList(booleanListArg:  ""false"" )
                }",
                t =>
                {
                    Assert.Equal(
                        "The specified argument value does not" +
                        " match the argument type.",
                        t.Message);
                    Assert.Equal("[Boolean!]", t.Extensions!["locationType"]);
                    Assert.Equal("booleanListArg", t.Extensions["argument"]);
                });
        }

        [Fact]
        public void CoercedIntIntoFloatArg()
        {
            ExpectValid(@"
                {
                    arguments {
                        ...coercedIntIntoFloatArg
                    }
                }

                fragment coercedIntIntoFloatArg on Arguments {
                    # Note: The input coercion rules for Float allow Int literals.
                    floatArgField(floatArg: 123)
                }
            ");
        }

        [Fact]
        public void GoodComplexDefaultValue()
        {
            ExpectValid(@"
                query goodComplexDefaultValue($search: ComplexInput = { name: ""Fido"" }) {
                    findDog(complex: $search)
                }
            ");
        }

        [Fact]
        public void StringIntoInt()
        {
            ExpectErrors(@"
                {
                    arguments {
                        ...stringIntoInt
                    }
                }

                fragment stringIntoInt on Arguments {
                    intArgField(intArg: ""123"")
                }
            ",
            t => Assert.Equal(
                    "The specified argument value does not match the " +
                    "argument type.",
                    t.Message));
        }

        [Fact]
        public void BadComplexValueArgument()
        {
            ExpectErrors(@"
                query badComplexValue {
                    findDog(complex: { name: 123 })
                }
            ",
            t => Assert.Equal(
                "The specified value type of field `name` " +
                "does not match the field type.",
                t.Message));
        }

        [Fact]
        public void BadComplexValueVariable()
        {
            ExpectErrors(@"
                query goodComplexDefaultValue($search: ComplexInput = { name: 123 }) {
                    findDog(complex: $search)
                }
            ",
            t => Assert.Equal(
                "The specified value type of field `name` " +
                    "does not match the field type.",
                    t.Message));
        }

        [Fact]
        public void BadValueVariable()
        {
            ExpectErrors(@"
                query goodComplexDefaultValue($search: ComplexInput = 123) {
                    findDog(complex: $search)
                }
            ",
            t => Assert.Equal(
                "The specified value type of variable `search` " +
                "does not match the variable type.",
                t.Message));
        }

        [Fact]
        public void GoodNullToIntNullableValue()
        {
            ExpectValid(@"
                {
                  arguments {
                    intArgField(intArg: null)
                  }
                }
            ");
        }

        [Fact]
        public void GoodIntValue()
        {
            ExpectValid(@"
                {
                  arguments {
                    intArgField(intArg: 2)
                  }
                }
            ");
        }

        [Fact]
        public void GoodIntNegativeValue()
        {
            ExpectValid(@"
                {
                  arguments {
                    intArgField(intArg: -2)
                  }
                }
            ");
        }

        [Fact]
        public void GoodNullToBooleanNullableValue()
        {
            ExpectValid(@"
                {
                  arguments {
                    booleanArgField(booleanArg: true)
                  }
                }
            ");
        }

        [Fact]
        public void GoodBooleanValue()
        {
            ExpectValid(@"
                {
                  arguments {
                    booleanArgField(booleanArg: true)
                  }
                }
            ");
        }

        [Fact]
        public void GoodStringValue()
        {
            ExpectValid(@"
                {
                  arguments {
                    stringArgField(stringArg: ""foo"")
                  }
                }
            ");
        }

        [Fact]
        public void GoodNullToStringNullableValue()
        {
            ExpectValid(@"
                {
                  arguments {
                    stringArgField(stringArg: null)
                  }
                }
            ");
        }

        [Fact]
        public void GoodNullToFloatNullableValue()
        {
            ExpectValid(@"
                {
                  arguments {
                    floatArgField(floatArg: null)
                  }
                }
            ");
        }

        [Fact]
        public void GoodFloatValue()
        {
            ExpectValid(@"
                {
                  arguments {
                    floatArgField(floatArg: 1.1)
                  }
                }
            ");
        }

        [Fact]
        public void GoodNegativeFloatValue()
        {
            ExpectValid(@"
                {
                  arguments {
                    floatArgField(floatArg: -1.1)
                  }
                }
            ");
        }

        [Fact]
        public void GoodIntToFloat()
        {
            ExpectValid(@"
                {
                  arguments {
                    floatArgField(floatArg: 1)
                  }
                }
            ");
        }

        [Fact]
        public void GoodIntToId()
        {
            ExpectValid(@"
                {
                  arguments {
                    idArgField(idArg: 1)
                  }
                }
            ");
        }

        [Fact]
        public void GoodStringToId()
        {
            ExpectValid(@"
                {
                  arguments {
                    idArgField(idArg: ""someIdString"")
                  }
                }
            ");
        }

        [Fact]
        public void GoodNullToIdNullable()
        {
            ExpectValid(@"
                {
                  arguments {
                    idArgField(idArg: null)
                  }
                }
            ");
        }

        [Fact]
        public void GoodEnumValue()
        {
            ExpectValid(@"
                {
                  arguments {
                    enumArgField(enumArg: SIT)
                  }
                }
            ");
        }

        [Fact]
        public void GoodNullToEnumNullableValue()
        {
            ExpectValid(@"
                {
                  arguments {
                    enumArgField(enumArg: null)
                  }
                }
            ");
        }

        [Fact]
        public void BadIntIntoString()
        {
            ExpectErrors(@"
                {
                    arguments {
                        stringArgField(stringArg: 1)
                    }
                }
            ");
        }

        [Fact]
        public void BadFloatIntoString()
        {
            ExpectErrors(@"
                {
                    arguments {
                        stringArgField(stringArg: 1.0)
                    }
                }
            ");
        }

        [Fact]
        public void BadBooleanIntoString()
        {
            ExpectErrors(@"
                {
                    arguments {
                        stringArgField(stringArg: true)
                    }
                }
            ");
        }

        [Fact]
        public void BadEnumIntoString()
        {
            ExpectErrors(@"
                {
                    arguments {
                        stringArgField(stringArg: BAR)
                    }
                }
            ");
        }

        [Fact]
        public void BadStringIntoInt()
        {
            ExpectErrors(@"
                {
                    arguments {
                        intArgField(intArg: ""3"")
                    }
                }
            ");
        }

        [Fact]
        public void BadBooleanIntoInt()
        {
            ExpectErrors(@"
                {
                    arguments {
                        intArgField(intArg: false)
                    }
                }
            ");
        }

        [Fact]
        public void BadEnumIntoInt()
        {
            ExpectErrors(@"
                {
                    arguments {
                        intArgField(intArg: BAR)
                    }
                }
            ");
        }

        [Fact]
        public void BadSimpleFloatIntoInt()
        {
            ExpectErrors(@"
                {
                    arguments {
                        intArgField(intArg: 3.0)
                    }
                }
            ");
        }

        [Fact]
        public void BadFloatIntoInt()
        {
            ExpectErrors(@"
                {
                    arguments {
                        intArgField(intArg: 3.333)
                    }
                }
            ");
        }

        [Fact]
        public void BadStringIntoFloat()
        {
            ExpectErrors(@"
                {
                    arguments {
                        floatArgField(floatArg: ""3.333"")
                    }
                }
            ");
        }

        [Fact]
        public void BadBooleanIntoFloat()
        {
            ExpectErrors(@"
                {
                    arguments {
                        floatArgField(floatArg: true)
                    }
                }
            ");
        }

        [Fact]
        public void BadEnumIntoFloat()
        {
            ExpectErrors(@"
                {
                    arguments {
                        floatArgField(floatArg: BAR)
                    }
                }
            ");
        }

        [Fact]
        public void BadStringIntoBool()
        {
            ExpectErrors(@"
                {
                    arguments {
                        intArgField(intArg: ""true"")
                    }
                }
            ");
        }

        [Fact]
        public void BadEnumIntoBool()
        {
            ExpectErrors(@"
                {
                    arguments {
                        booleanArgField(booleanArg: BAR)
                    }
                }
            ");
        }

        [Fact]
        public void BadSimpleFloatIntoBool()
        {
            ExpectErrors(@"
                {
                    arguments {
                        booleanArgField(booleanArg: 3.0)
                    }
                }
            ");
        }

        [Fact]
        public void BadFloatIntoBool()
        {
            ExpectErrors(@"
                {
                    arguments {
                        booleanArgField(booleanArg: 3.333)
                    }
                }
            ");
        }

        [Fact]
        public void BadFloatIntoId()
        {
            ExpectErrors(@"
                {
                    arguments {
                        idArgField(idArg: 1.0)
                    }
                }
            ");
        }

        [Fact]
        public void BadBooleanIntoId()
        {
            ExpectErrors(@"
                {
                    arguments {
                        idArgField(idArg: true)
                    }
                }
            ");
        }

        [Fact]
        public void BadEnumIntoId()
        {
            ExpectErrors(@"
                {
                    arguments {
                        idArgField(idArg: TRUE)
                    }
                }
            ");
        }

        [Fact]
        public void BadIntIntoEnum()
        {
            ExpectErrors(@"
                {
                    arguments {
                        enumArgField(enumArg: 2)
                    }
                }
            ");
        }

        [Fact]
        public void BadFloatIntoEnum()
        {
            ExpectErrors(@"
                {
                    arguments {
                        enumArgField(enumArg: 1.0)
                    }
                }
            ");
        }

        // is this something that we find ok or should we change it back?
        [Fact(Skip = "We do allow this at the moment.")]
        public void BadStringIntoEnum()
        {
            ExpectErrors(@"
                {
                    arguments {
                        enumArgField(enumArg: ""SIT"")
                    }
                }
            ");
        }

        [Fact]
        public void BadBooleanIntoEnum()
        {
            ExpectErrors(@"
                {
                    arguments {
                        enumArgField(enumArg: true)
                    }
                }
            ");
        }

        [Fact]
        public void BadUnknowEnumIntoEnum()
        {
            ExpectErrors(@"
                {
                    arguments {
                        enumArgField(enumArg: HELLO)
                    }
                }
            ");
        }

        [Fact]
        public void BadWrongCasingEnumIntoEnum()
        {
            ExpectErrors(@"
                {
                    arguments {
                        enumArgField(enumArg: sit)
                    }
                }
            ");
        }

        [Fact(Skip = "This really should be caught! " +
                     "=> Spec issue http://spec.graphql.org/draft/#sel-JALTHHDHFFCAACEQl_M")]
        public void BadNullToString()
        {
            ExpectErrors(@"
                query InvalidItem {
                    nonNull(a: null)
                }
            ");
        }

        [Fact]
        public void GoodListValue()
        {
            ExpectValid(@"
                {
                    arguments {
                        stringListArgField(stringListArg: [""one"", null, ""two""])
                    }
                }
            ");
        }

        [Fact]
        public void GoodEmptyListValue()
        {
            ExpectValid(@"
                {
                    arguments {
                        stringListArgField(stringListArg: [])
                    }
                }
            ");
        }

        [Fact]
        public void GoodNullListValue()
        {
            ExpectValid(@"
                {
                    arguments {
                        stringListArgField(stringListArg: null)
                    }
                }
            ");
        }

        [Fact]
        public void GoodSingleValueListValue()
        {
            ExpectValid(@"
                {
                    arguments {
                        stringListArgField(stringListArg: ""singleValueInList"")
                    }
                }
            ");
        }

        [Fact]
        public void BadIncorrectItemType()
        {
            ExpectErrors(@"
                {
                    arguments {
                        stringListArgField(stringListArg: [""one"", 2])
                    }
                }
            ");
        }

        [Fact]
        public void BadSingleValueInvalid()
        {
            ExpectErrors(@"
                {
                    arguments {
                        stringListArgField(stringListArg: 2)
                    }
                }
            ");
        }

        [Fact]
        public void GoodArgOnOptionalArg()
        {
            ExpectValid(@"
                {
                    dog {
                        isHouseTrained(atOtherHomes: true)
                    }
                }
            ");
        }

        [Fact]
        public void GoodNoArgOnOptionalArg()
        {
            ExpectValid(@"
                {
                    dog {
                        isHouseTrained
                    }
                }
            ");
        }

        [Fact]
        public void GoodMultipleArgs()
        {
            ExpectValid(@"
                {
                    arguments {
                        multipleReqs(x: 1, y: 2)
                    }
                }
            ");
        }

        [Fact]
        public void GoodMultipleArgsReversed()
        {
            ExpectValid(@"
                {
                    arguments {
                        multipleReqs(y: 2, x: 1)
                    }
                }
            ");
        }

        [Fact]
        public void GoodNoMultipleArgsOps()
        {
            ExpectValid(@"
                {
                    arguments {
                        multipleOpts
                    }
                }
            ");
        }

        [Fact]
        public void GoodOneMultipleArgsOps()
        {
            ExpectValid(@"
                {
                    arguments {
                        multipleOpts(opt1: 1)
                    }
                }
            ");
        }

        [Fact]
        public void GoodSecondOneMultipleArgsOps()
        {
            ExpectValid(@"
                {
                    arguments {
                        multipleOpts(opt2: 1)
                    }
                }
            ");
        }

        [Fact]
        public void GoodMultipleRequiredArgsOnMixedList()
        {
            ExpectValid(@"
                {
                    arguments {
                        multipleOptsAndReqs(req1: 3, req2: 4)
                    }
                }
            ");
        }

        [Fact]
        public void GoodMultipleRequiredArgsOnMixedOneOptionalList()
        {
            ExpectValid(@"
                {
                    arguments {
                        multipleOptsAndReqs(req1: 3, req2: 4, opt1: 1)
                    }
                }
            ");
        }

        [Fact]
        public void GoodMultipleRequiredArgsOnMixedAllOptionalList()
        {
            ExpectValid(@"
                {
                    arguments {
                        multipleOptsAndReqs(req1: 3, req2: 4, opt1: 1, opt2: 2)
                    }
                }
            ");
        }

        [Fact]
        public void BadMultipleIncorrectValueType()
        {
            ExpectErrors(@"
                {
                    arguments {
                        multipleReqs(x: ""two"", y: ""one"")
                    }
                }
            ");
        }

        [Fact]
        public void GoodOptionalArgDespiteRequiredFieldInType()
        {
            ExpectValid(@"
                {
                    arguments {
                        complexArgField
                    }
                }
            ");
        }

        [Fact]
        public void GoodPartialObjectOnlyRequired()
        {
            ExpectValid(@"
                {
                    arguments {
                        complexArgField(complexArg: { requiredField: true })
                    }
                }
            ");
        }

        [Fact]
        public void GoodPartialObjectOnlyRequiredCanBeFalse()
        {
            ExpectValid(@"
                {
                    arguments {
                        complexArgField(complexArg: { requiredField: false })
                    }
                }
            ");
        }

        [Fact]
        public void GoodPartialObjectIncludingRequired()
        {
            ExpectValid(@"
                {
                    arguments {
                        complexArgField(complexArg: { requiredField: true, intField: 4 })
                    }
                }
            ");
        }

        [Fact]
        public void GoodComplexFullObject()
        {
            ExpectValid(@"
                {
                    arguments {
                        complexArgField(complexArg: {
                        requiredField: true,
                        intField: 4,
                        stringField: ""foo"",
                        booleanField: false,
                        stringListField: [""one"", ""two""]
                        })
                    }
                }
            ");
        }

        [Fact]
        public void GoodComplexFullDfferentOrderObject()
        {
            ExpectValid(@"
                {
                    arguments {
                        complexArgField(complexArg: {
                            stringListField: [""one"", ""two""],
                            booleanField: false,
                            requiredField: true,
                            stringField: ""foo"",
                            intField: 4,
                        })
                    }
                }
            ");
        }

        [Fact]
        public void BadComplexInputInvalidElementType()
        {
            ExpectErrors(@"
                {
                    arguments {
                        complexArgField(complexArg: {
                            stringListField: [""one"", 2],
                            requiredField: true,
                        })
                    }
                }
            ");
        }

        [Fact]
        public void BadUnknownFieldOnComplexType()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    complicatedArgs {
                        complexArgField(complexArg: {
                        requiredField: true,
                        invalidField: ""value""
                        })
                    }
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.True(context.UnexpectedErrorsDetected);
        }

        [Fact]
        public void BadCustomerScalarIsInvalid()
        {
            ExpectErrors(@"
                {
                   invalidArg(arg: 123)
                }
            ");
        }

        [Fact]
        public void GoodCustomerScalarAcceptsComplexLiterals()
        {
            ExpectValid(@"
                {
                    test1: anyArg(arg: 123)
                    test2: anyArg(arg: ""abc"")
                    test3: anyArg(arg: [123, ""abc""])
                    test4: anyArg(arg: {deep: [123, ""abc""]})
                }
            ");
        }

        [Fact]
        public void GoodDirectiveValidTypes()
        {
            ExpectValid(@"
                {
                    dog @include(if: true) {
                        name
                    }
                    human @skip(if: false) {
                        name
                    }
                }
            ");
        }

        [Fact]
        public void GoodDirectiveAnyTypes()
        {
            ExpectValid(@"
                {
                    dog @complex(anyArg: 123)
                        @complex(anyArg: ""abc"")
                        @complex(anyArg: [123, ""abc""])
                        @complex(anyArg: {deep: [123, ""abc""]}) {
                        name
                    }
                }
            ");
        }

        [Fact]
        public void BadDirectiveInvalidTypes()
        {
            ExpectErrors(@"
               {
                    dog @include(if: ""yes"") {
                        name @skip(if: ENUM)
                    }
                }
            ");
        }

        [Fact]
        public void GoodQueryVariablesDefaultValues()
        {
            ExpectValid(@"
                query WithDefaultValues(
                    $a: Int = 1,
                    $b: String = ""ok"",
                    $c: ComplexInput3TypeInput = { requiredField: true, intField: 3 }
                    $d: Int! = 123
                    ) {
                dog { name }
                }
            ");
        }

        [Fact]
        public void GoodQueryVariablesDefaultNullValues()
        {
            ExpectValid(@"
                query WithDefaultValues(
                    $a: Int = null,
                    $b: String = null,
                    $c: ComplexInput3TypeInput = { requiredField: true, intField: null }
                    ) {
                    dog { name }
                }
            ");
        }

        [Fact]
        public void GoodQueryVariablesDefaultAnyValues()
        {
            ExpectValid(@"
                query WithDefaultValues(
                    $test1: Any =  123
                    $test2: Any =  ""abc""
                    $test3: Any =  [123, ""abc""]
                    $test4: Any =  {deep: [123, ""abc""]}
                    ) {
                    dog { name }
                }
            ");
        }

        [Fact]
        public void BadVariablesWithInvalidDefaultValues()
        {
            ExpectErrors(@"
                query WithDefaultValues(
                    $a: Int! = null,
                    $b: String! = null,
                    $c: ComplexInput = { requiredField: null, intField: null }
                    ) {
                    dog { name }
                }
            ");
        }

        [Fact]
        public void BadVariablesWithInvalidDefaultValuesTypes()
        {
            ExpectErrors(@"
                query InvalidDefaultValues(
                    $a: Int = ""one"",
                    $b: String = 4,
                    $c: ComplexInput = ""NotVeryComplex""
                    ) {
                    dog { name }
                }
            ");
        }

        [Fact]
        public void BadVariablesWithInvalidComplexDefaultValues()
        {
            ExpectErrors(@"
                query WithDefaultValues(
                        $a: ComplexInput = { requiredField: 123, intField: ""abc"" }
                    ) {
                    dog { name }
                }
            ");
        }

        [Fact]
        public void BadVariablesComplexVariableMissingRequiredField()
        {
            ExpectErrors(@"
                query MissingRequiredField($a: ComplexInput = {intField: 3}) {
                    dog { name }
                }
            ");
        }

        [Fact]
        public void BadVariablesListWithInvalidItem()
        {
            ExpectErrors(@"
                query InvalidItem($a: [String] = [""one"", 2]) {
                    dog { name }
                }
            ");
        }
    }
}
