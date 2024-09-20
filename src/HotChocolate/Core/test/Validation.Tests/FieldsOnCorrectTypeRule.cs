using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class FieldsOnCorrectTypeRuleTests
    : DocumentValidatorVisitorTestBase
{
    public FieldsOnCorrectTypeRuleTests()
        : base(services => services.AddFieldRules())
    {
    }

    [Fact]
    public void GoodObjectFieldSelection()
    {
        ExpectValid(@"
                fragment objectFieldSelection on Dog {
                    __typename
                    name
                }

                query {
                    dog {
                         ...objectFieldSelection
                    }
                }
            ");
    }

    [Fact]
    public void GoodAliasedObjectFieldSelection()
    {
        ExpectValid(@"
                fragment aliasedObjectFieldSelection on Dog {
                    tn : __typename
                    otherName : name
                }

                query {
                    dog {
                         ...aliasedObjectFieldSelection
                    }
                }
            ");
    }

    [Fact]
    public void GoodInterfacesObjectFieldSelection()
    {
        ExpectValid(@"
                fragment interfaceFieldSelection on Pet {
                    otherName : name
                }

                query {
                    pet {
                         ...interfaceFieldSelection
                    }
                }
            ");
    }

    [Fact]
    public void BadReportsErrorWhenTypeIsKnown()
    {
        ExpectErrors(@"
                fragment typeKnownAgain on Pet {
                    unknown_pet_field {
                        ... on Cat {
                            unknown_cat_field
                        }
                    }
                }
                query {
                    pet {
                         ...typeKnownAgain
                    }
                }
            ");
    }

    [Fact]
    public void BadFieldNotDefinedOnFragement()
    {
        ExpectErrors(@"
                fragment fieldNotDefined on Dog {
                    meowVolume
                }

                query {
                    dog {
                         ...fieldNotDefined
                    }
                }
            ");
    }

    [Fact]
    public void BadIgnoresDeeplyUnknownField()
    {
        ExpectErrors(@"
                fragment deepFieldNotDefined on Dog {
                    unknown_field {
                        deeper_unknown_field
                    }
                }

                query {
                    dog {
                         ...deepFieldNotDefined
                    }
                }
            ");
    }

    [Fact]
    public void BadSubFieldNotDefined()
    {
        ExpectErrors(@"
                fragment subFieldNotDefined on Human {
                    pets {
                        unknown_field
                    }
                }

                query {
                    human {
                         ...subFieldNotDefined
                    }
                }
            ");
    }

    [Fact]
    public void BadFieldNotDefinedOnInlineFragment()
    {
        ExpectErrors(@"
                fragment fieldNotDefined on Pet {
                    ... on Dog {
                        meowVolume
                    }
                }

                query {
                    pet {
                         ...fieldNotDefined
                    }
                }
            ");
    }

    [Fact]
    public void BadAliasedFieldTargetNotDefined()
    {
        ExpectErrors(@"
                fragment aliasedFieldTargetNotDefined on Dog {
                    volume : mooVolume
                }

                query {
                    dog {
                         ...aliasedFieldTargetNotDefined
                    }
                }
            ");
    }

    [Fact]
    public void BadAliasedLyingFieldTargetNotDefined()
    {
        ExpectErrors(@"
                fragment aliasedLyingFieldTargetNotDefined on Dog {
                    barkVolume : kawVolume
                }

                query {
                    dog {
                         ...aliasedLyingFieldTargetNotDefined
                    }
                }
            ");
    }

    [Fact]
    public void BadNotDefinedOnInterface()
    {
        ExpectErrors(@"
                fragment notDefinedOnInterface on Pet {
                    tailLength
                }

                query {
                    pet {
                         ...notDefinedOnInterface
                    }
                }
            ");
    }

    [Fact]
    public void DefinedOnImplementorsButNotOnInterface()
    {
        ExpectErrors(@"
                fragment definedOnImplementorsButNotInterface on Pet {
                    nickname
                }

                query {
                    pet {
                         ...definedOnImplementorsButNotInterface
                    }
                }
            ");
    }

    [Fact]
    public void MetaFieldSelectionOnUnion()
    {
        ExpectValid(@"
                fragment directFieldSelectionOnUnion on CatOrDog {
                  __typename
                }

                query {
                    catOrDog {
                         ...directFieldSelectionOnUnion
                    }
                }
            ");
    }

    [Fact]
    public void DireftFieldSelectionOnUnion()
    {
        ExpectErrors(@"
                fragment directFieldSelectionOnUnion on CatOrDog {
                    directField
                }

                query {
                    catOrDog {
                         ...directFieldSelectionOnUnion
                    }
                }
            ");
    }

    [Fact]
    public void DefinedOnImplementorQueriedOnUnion()
    {
        ExpectErrors(@"
               fragment definedOnImplementorsQueriedOnUnion on CatOrDog {
                    name
                }

                query {
                    catOrDog {
                         ...definedOnImplementorsQueriedOnUnion
                    }
                }
            ");
    }

    [Fact]
    public void FieldInInlineFragment()
    {
        ExpectValid(@"
                fragment objectFieldSelection on Pet {
                    ... on Dog {
                        name
                    }
                    ... {
                        name
                    }
                }

                query {
                    pet {
                         ...objectFieldSelection
                    }
                }
            ");
    }

    [Fact]
    public void WrongFieldsOnUnionTypeList()
    {
        // arrange
        var schema = SchemaBuilder
            .New()
            .AddDocumentFromString(@"
                    type Bar { baz: String }
                    type Baz { baz: String }
                    union Foo = Bar | Baz
                    type Query {
                        list: [Foo!]
                    }")
            .AddResolver("Query", "list", ctx => null!)
            .AddResolver("Bar", "baz", ctx => null!)
            .AddResolver("Baz", "baz", ctx => null!)
            .Create();

        ExpectErrors(
            schema,
            @"
                query {
                    list {
                        qux
                    }
                }
            ");
    }
}
