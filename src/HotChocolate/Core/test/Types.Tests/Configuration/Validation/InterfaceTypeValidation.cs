using Xunit;

namespace HotChocolate.Configuration.Validation
{
    public class InterfaceTypeValidation : TypeValidationTestBase
    {
        [Fact]
        public void Fields_With_Two_Underscores_Are_Not_Allowed()
        {
            ExpectError(@"
                type Query {
                    foo : FooInterface
                }

                interface FooInterface {
                    __foo : String
                }

                type Foo implements FooInterface {
                    __foo : String
                }
            ");
        }

        [Fact]
        public void Arguments_With_Two_Underscores_Are_Not_Allowed()
        {
            ExpectError(@"
                type Query {
                    foo : FooInterface
                }

                interface FooInterface {
                    foo(__bar: String) : String
                }

                type Foo implements FooInterface {
                    foo(__bar: String) : String
                }
            ");
        }

        [Fact]
        public void Interface_Implements_Not_The_Interfaces_Of_Its_Interfaces()
        {
            ExpectError(@"
                type Query {
                    foo: Foo
                }

                interface C {
                    def : String
                }

                interface B implements C {
                    cde: String
                    def : String
                }

                interface A implements B {
                    abc(a: String): String
                    cde: String
                    def : String
                }

                type Foo implements A & B & C {
                    abc(a: String): String
                    def: String
                    cde: String
                }
            ");
        }
    }
}
