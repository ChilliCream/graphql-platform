using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class ValuesOfCorrectTypeRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public ValuesOfCorrectTypeRuleTests()
            : base(services => services.AddInputObjectRules())
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
            // arrange
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
                     Assert.Equal("Boolean!", t.Extensions["locationType"]);
                     Assert.Equal("booleanListArg", t.Extensions["argument"]);
                 });
        }

        [Fact]
        public void BadBooleanListArgString()
        {
            // arrange
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
                    Assert.Equal("[Boolean!]", t.Extensions["locationType"]);
                    Assert.Equal("booleanListArg", t.Extensions["argument"]);
                });
        }

        [Fact]
        public void CoercedIntIntoFloatArg()
        {
            // arrange
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
            // arrange
            ExpectValid(@"
                query goodComplexDefaultValue($search: ComplexInput = { name: ""Fido"" }) {
                    findDog(complex: $search)
                }
            ");
        }

        [Fact]
        public void StringIntoInt()
        {
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
            ExpectValid(@"
                {
                  arguments {
                    enumArgField(enumArg: null)
                  }
                }
            ");
        }
    }
}
