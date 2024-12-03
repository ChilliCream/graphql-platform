using HotChocolate.Execution;
using HotChocolate.Language.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class DescriptionTests
{
    [Fact]
    public async Task Schema_With_All_Possible_Descriptions()
    {
        // arrange
        var schema = await GetSchemaWithAllPossibleDescriptionsAsync();

        // act
        // assert
        SchemaPrinter
            .PrintSchema(schema)
            .Print(indented: true)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Schema_With_All_Possible_Descriptions_No_Indent()
    {
        // arrange
        var schema = await GetSchemaWithAllPossibleDescriptionsAsync();

        // act
        // assert
        SchemaPrinter
            .PrintSchema(schema)
            .Print(indented: false)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Linebreak_In_Description()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .UseField(_ => _)
            .AddQueryType(d => d
                .Field("field").Type<StringType>()
                .Description("Comment with manual\nline break"))
            .BuildSchemaAsync();

        // act
        // assert
        schema.ToString().MatchInlineSnapshot(""""
                                              schema {
                                                query: Query
                                              }

                                              type Query {
                                                """
                                                Comment with manual
                                                line break
                                                """
                                                field: String
                                              }
                                              """");
    }

    [Fact]
    public async Task ConsecutiveLinebreaks_In_Description()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .UseField(_ => _)
            .AddQueryType(d => d
                .Field("field").Type<StringType>()
                .Description("Comment with manual\n\nline breaks"))
            .BuildSchemaAsync();

        // act
        // assert
        schema.ToString().MatchInlineSnapshot(""""
                                              schema {
                                                query: Query
                                              }

                                              type Query {
                                                """
                                                Comment with manual

                                                line breaks
                                                """
                                                field: String
                                              }
                                              """");
    }

    [Fact]
    public async Task Whitespace_At_Start_And_End_Singleline()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .UseField(_ => _)
            .AddQueryType(d => d
                .Field("field").Type<StringType>()
                .Description("   Single line comment    "))
            .BuildSchemaAsync();

        // act
        // assert
        schema.ToString().MatchInlineSnapshot("""
                                              schema {
                                                query: Query
                                              }

                                              type Query {
                                                "Single line comment"
                                                field: String
                                              }
                                              """);
    }

    [Fact]
    public async Task Whitespace_At_Start_And_End_Multiline()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .UseField(_ => _)
            .AddQueryType(d => d
                .Field("field").Type<StringType>()
                .Description("   Multi line\ncomment    "))
            .BuildSchemaAsync();

        // act
        // assert
        schema.ToString().MatchInlineSnapshot(""""
                                              schema {
                                                query: Query
                                              }

                                              type Query {
                                                """
                                                Multi line
                                                comment
                                                """
                                                field: String
                                              }
                                              """");
    }

    [Fact]
    public async Task Whitespace_In_Multiline_Description()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .UseField(_ => _)
            .AddQueryType(d => d
                .Field("field").Type<StringType>()
                .Description("""
                             Multi
                               line
                             description
                             """))
            .BuildSchemaAsync();

        // act
        // assert
        schema.ToString().MatchInlineSnapshot(""""
                                              schema {
                                                query: Query
                                              }

                                              type Query {
                                                """
                                                Multi
                                                  line
                                                description
                                                """
                                                field: String
                                              }
                                              """");
    }

    [Fact]
    public async Task Linebreak_At_End_Of_Description()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .UseField(_ => _)
            .AddQueryType(d => d
                .Field("field").Type<StringType>()
                .Description("Single line with linebreak at end\n"))
            .BuildSchemaAsync();

        // act
        // assert
        schema.ToString().MatchInlineSnapshot(""""
                                              schema {
                                                query: Query
                                              }

                                              type Query {
                                                "Single line with linebreak at end"
                                                field: String
                                              }
                                              """");
    }

    private static async Task<ISchema> GetSchemaWithAllPossibleDescriptionsAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.RemoveUnreachableTypes = false;
                o.RemoveUnusedTypeSystemDirectives = false;
                o.EnableTag = false;
            })
            .SetSchema(d => d.Description("Single line comment"))
            .AddQueryType<Query>()
            .AddInterfaceType<ISomeInterface>()
            .AddInterfaceType<IOtherInterface>()
            .AddObjectType<OtherObjectType>()
            .AddUnionType<ISomeUnion>()
            .AddUnionType<IOtherUnion>()
            .AddDirectiveType<SomeDirective>()
            .AddDirectiveType<OtherDirective>()
            .AddType<SomeEnum>()
            .AddType<OtherEnum>()
            .AddType<SomeScalar>()
            .AddType<OtherScalar>()
            .BuildSchemaAsync();
    }

    [GraphQLDescription("""
                        Multi line
                        comment
                        """)]
    public class Query
    {
        [GraphQLDescription("Single line comment")]
        public string OutputFieldSingle() => default;

        [GraphQLDescription("""
                            Multi line
                            comment
                            """)]
        public string OutputFieldMulti() => default;

        public string OutputFieldWithArgs(
            [GraphQLDescription("Single line comment")] SomeInput arg1,
            [GraphQLDescription("""
                                Multi line
                                comment
                                """)] OtherInput arg2) => default;
    }

    [InputObjectType]
    [GraphQLDescription("""
                        Multi line
                        comment
                        """)]
    public class SomeInput
    {
        [GraphQLDescription("Single line comment")]
        public string Field { get; set; }

        [GraphQLDescription("""
                            Multi line
                            comment
                            """)]
        public string FieldMulti { get; set; }
    }

    [InputObjectType]
    [GraphQLDescription("Single line comment")]
    public class OtherInput
    {
        [GraphQLDescription("Single line comment")]
        public string Field { get; set; }

        [GraphQLDescription("""
                            Multi line
                            comment
                            """)]
        public string FieldMulti { get; set; }
    }

    [UnionType("SomeUnion")]
    [GraphQLDescription("""
                        Multi line
                        comment
                        """)]
    public interface ISomeUnion
    {
    }

    [UnionType("OtherUnion")]
    [GraphQLDescription("Single line comment")]
    public interface IOtherUnion
    {
    }

    [InterfaceType("SomeInterface")]
    [GraphQLDescription("""
                        Multi line
                        comment
                        """)]
    public interface ISomeInterface
    {
        [GraphQLDescription("Single line comment")]
        string Field();

        [GraphQLDescription("""
                            Multi line
                            comment
                            """)]
        string FieldMulti();

        string FieldWithArgs(
            [GraphQLDescription("Single line comment")] string arg1,
            [GraphQLDescription("""
                                Multi line
                                comment
                                """)] string arg2);
    }

    [InterfaceType("OtherInterface")]
    [GraphQLDescription("Single line comment")]
    public interface IOtherInterface
    {
        string Field();
    }

    [GraphQLDescription("""
                        Multi line
                        comment
                        """)]
    public class OtherObjectType : ISomeUnion, IOtherUnion, ISomeInterface, IOtherInterface
    {
        public string Field() => default;

        public string FieldMulti() => default;
        public string FieldWithArgs(string arg1, string arg2) => default;
    }

    public class SomeDirective : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Location(DirectiveLocation.Field);
            descriptor.Description("""
                                   Multi line
                                   comment
                                   """);

            descriptor.Argument("arg1").Type<StringType>().Description("Single line comment");
            descriptor.Argument("arg2").Type<StringType>().Description("""
                                                                       Multi line
                                                                       comment
                                                                       """);
        }
    }

    public class OtherDirective : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Location(DirectiveLocation.Field);
            descriptor.Description("Single line comment");

            descriptor.Argument("arg1").Type<StringType>().Description("Single line comment");
            descriptor.Argument("arg2").Type<StringType>().Description("""
                                                                       Multi line
                                                                       comment
                                                                       """);
        }
    }

    public class SomeEnum : EnumType
    {
        protected override void Configure(IEnumTypeDescriptor descriptor)
        {
            descriptor.Description("Single line comment");

            descriptor.Value("VALUE1").Description("Single line comment");
            descriptor.Value("VALUE2").Description("""
                                                   Multi line
                                                   comment
                                                   """);
        }
    }

    public class OtherEnum : EnumType
    {
        protected override void Configure(IEnumTypeDescriptor descriptor)
        {
            descriptor.Description("""
                                   Multi line
                                   comment
                                   """);

            descriptor.Value("VALUE1").Description("Single line comment");
            descriptor.Value("VALUE2").Description("""
                                                   Multi line
                                                   comment
                                                   """);
        }
    }

    public class SomeScalar : StringType
    {
        public SomeScalar() : base("Some", "Single line comment")
        {
        }
    }

    public class OtherScalar : StringType
    {
        public OtherScalar() : base("Other", """
                                           Multi line
                                           comment
                                           """)
        {
        }
    }
}
