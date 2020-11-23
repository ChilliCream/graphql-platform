using Xunit;

namespace HotChocolate.Configuration.Validation
{
    public class ObjectTypeValidation : TypeValidationTestBase
    {
        [Fact]
        public void Fields_With_Two_Underscores_Are_Not_Allowed()
        {
            ExpectError(@"
                type Query {
                    __foo : String
                }
            ");
        }

        [Fact]
        public void Arguments_With_Two_Underscores_Are_Not_Allowed()
        {
            ExpectError(@"
                type Query {
                    foo(__bar: String) : String
                }
            ");
        }

        [Fact]
        public void Implemented_Field_Is_UnionType_Field_Is_ObjectType()
        {
            ExpectValid(@"
                type Query {
                    foo: Foo
                }

                interface FooInterface {
                    barOrBaz: BarOrBaz
                }

                type Foo implements FooInterface {
                    barOrBaz: Bar
                }

                union BarOrBaz = Bar | Baz

                type Bar {
                    bar: String
                }

                type Baz {
                    baz: String
                }
            ");
        }

        [Fact]
        public void Implemented_Field_Is_Interface_Field_Is_ObjectType()
        {
            ExpectValid(@"
                type Query {
                    foo: Foo
                }

                interface FooInterface {
                    bar: BarInterface
                }

                type Foo implements FooInterface {
                    bar: Bar
                }

                interface BarInterface {
                    bar: String
                }

                type Bar implements BarInterface {
                    bar: String
                }
            ");
        }

        [Fact]
        public void Implemented_Field_Is_Interface_List_Field_Is_ObjectType_List()
        {
            ExpectValid(@"
                type Query {
                    foo: Foo
                }

                interface FooInterface {
                    bar: [BarInterface]
                }

                type Foo implements FooInterface {
                    bar: [Bar]
                }

                interface BarInterface {
                    bar: String
                }

                type Bar implements BarInterface {
                    bar: String
                }
            ");
        }

        [Fact]
        public void Implemented_Field_Is_Interface_Field_Is_NonNull_ObjectType()
        {
            ExpectValid(@"
                type Query {
                    foo: Foo
                }

                interface FooInterface {
                    bar: BarInterface
                }

                type Foo implements FooInterface {
                    bar: Bar!
                }

                interface BarInterface {
                    bar: String
                }

                type Bar implements BarInterface {
                    bar: String
                }
            ");
        }

        [Fact]
        public void Implemented_Field_Is_Nullable_Field_Is_NonNull()
        {
            ExpectValid(@"
                type Query {
                    foo: Foo
                }

                interface FooInterface {
                    bar: String
                }

                type Foo implements FooInterface {
                    bar: String!
                }
            ");
        }

        [Fact]
        public void Implemented_Field_Is_NonNull_Field_Is_Nullable()
        {
            ExpectError(@"
                type Query {
                    foo: Foo
                }

                interface FooInterface {
                    bar: String!
                }

                type Foo implements FooInterface {
                    bar: String
                }
            ");
        }

        [Fact]
        public void All_Arguments_Are_Implemented()
        {
            ExpectValid(@"
                type Query {
                    foo: Foo
                }

                interface FooInterface {
                    abc(a: String): String
                }

                type Foo implements FooInterface {
                    abc(a: String): String
                }
            ");
        }

        [Fact]
        public void Field_Has_Additional_Arguments()
        {
            ExpectValid(@"
                type Query {
                    foo: Foo
                }

                interface FooInterface {
                    abc(a: String): String
                }

                type Foo implements FooInterface {
                    abc(a: String b:String): String
                }
            ");
        }

        [Fact]
        public void Field_Has_Additional_NonNull_Arguments()
        {
            ExpectError(@"
                type Query {
                    foo: Foo
                }

                interface FooInterface {
                    abc(a: String): String
                }

                type Foo implements FooInterface {
                    abc(a: String b:String!): String
                }
            ");
        }

        [Fact]
        public void Arguments_Are_Not_Implemented()
        {
            ExpectError(@"
                type Query {
                    foo: Foo
                }

                interface FooInterface {
                    abc(a: String): String
                }

                type Foo implements FooInterface {
                    abc: String
                }
            ");
        }

        [Fact]
        public void Implemented_Argument_Types_Do_Not_Match()
        {
            ExpectError(@"
                type Query {
                    foo: Foo
                }

                interface FooInterface {
                    abc(a: String): String
                }

                type Foo implements FooInterface {
                    abc(a: String!): String
                }
            ");
        }

        [Fact]
        public void Object_Implements_All_Interfaces()
        {
            ExpectValid(@"
                type Query {
                    foo: Foo
                }

                interface B {
                    cde: String
                }

                interface A implements B {
                    abc(a: String): String
                    cde: String
                }

                type Foo implements A & B {
                    abc(a: String): String
                    cde: String
                }
            ");
        }

        [Fact]
        public void Object_Implements_Not_The_Interfaces_Of_Its_Interfaces()
        {
            ExpectError(@"
                type Query {
                    foo: Foo
                }

                interface B {
                    cde: String
                }

                interface A implements B {
                    abc(a: String): String
                }

                type Foo implements A & B {
                    abc(a: String): String
                }
            ");
        }

        [Fact]
        public void Object_Implements_Not_The_Interfaces_Of_Its_Interfaces_2()
        {
            ExpectError(@"
                type Query {
                    foo: Foo
                }

                interface B {
                    cde: String
                }

                interface A implements B {
                    abc(a: String): String
                }

                type Foo implements A {
                    abc(a: String): String
                }
            ");
        }
    }
}
