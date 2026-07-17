using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class PolicyTests
{
    [Fact]
    public static async Task Policy_Attribute_Should_Emit_Directives_When_Applied_To_Object_Type_And_Fields()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query
            }

            type Query @policy(names: "hasAccess") {
              financeReport: String! @policy(names: [["isAdmin", "isFinance"], ["isOwner"]])
              auditLog: String! @policy(names: "isAdmin", onDenied: ERROR)
              salary: String!
                @policy(names: "isAdmin")
                @policy(names: "isFinance", onDenied: ABORT)
            }

            "Defines the consequence that applies when a policy expression denies access."
            enum PolicyDenialBehavior {
              "The guarded value is set to null without an error."
              NULL
              "The guarded value is set to null and an authorization error is added."
              ERROR
              "The request is terminated."
              ABORT
            }

            """
            The @policy directive restricts access to the annotated type or field with a policy
            expression in disjunctive normal form. Names within an inner list combine with AND,
            the outer list combines with OR. The expression [["a", "b"], ["c"]] reads as
            (a AND b) OR c. Access is granted only when the expression evaluates to true.


            The onDenied argument defines the consequence when the expression does not evaluate
            to true: NULL sets the guarded value to null without an error, ERROR sets it to null
            and adds an authorization error, ABORT terminates the request.


            Repeated applications on the same member combine with AND and the most severe
            consequence wins.


            directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior! = NULL) repeatable on OBJECT | INTERFACE | FIELD_DEFINITION
            """
            directive @policy(
              "The policy expression in disjunctive normal form. Names within an inner list combine with AND, the outer list combines with OR."
              names: [[String!]!]!
              "The consequence that applies when the policy expression denies access."
              onDenied: PolicyDenialBehavior! = NULL
            ) repeatable on OBJECT | FIELD_DEFINITION | INTERFACE
            """");
    }

    [Fact]
    public static async Task Policy_Attribute_Should_Emit_Directives_When_Applied_To_Interface_Type_And_Fields()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithInterface>()
                .AddType<Document>()
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: QueryWithInterface
            }

            type QueryWithInterface {
              secured: Secured!
            }

            type Document implements Secured {
              token: String!
            }

            interface Secured @policy(names: "hasAccess") {
              token: String!
                @policy(names: [["isAdmin", "isFinance"], ["isOwner"]], onDenied: ABORT)
            }

            "Defines the consequence that applies when a policy expression denies access."
            enum PolicyDenialBehavior {
              "The guarded value is set to null without an error."
              NULL
              "The guarded value is set to null and an authorization error is added."
              ERROR
              "The request is terminated."
              ABORT
            }

            """
            The @policy directive restricts access to the annotated type or field with a policy
            expression in disjunctive normal form. Names within an inner list combine with AND,
            the outer list combines with OR. The expression [["a", "b"], ["c"]] reads as
            (a AND b) OR c. Access is granted only when the expression evaluates to true.


            The onDenied argument defines the consequence when the expression does not evaluate
            to true: NULL sets the guarded value to null without an error, ERROR sets it to null
            and adds an authorization error, ABORT terminates the request.


            Repeated applications on the same member combine with AND and the most severe
            consequence wins.


            directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior! = NULL) repeatable on OBJECT | INTERFACE | FIELD_DEFINITION
            """
            directive @policy(
              "The policy expression in disjunctive normal form. Names within an inner list combine with AND, the outer list combines with OR."
              names: [[String!]!]!
              "The consequence that applies when the policy expression denies access."
              onDenied: PolicyDenialBehavior! = NULL
            ) repeatable on OBJECT | FIELD_DEFINITION | INTERFACE
            """");
    }

    [Fact]
    public static async Task Policy_Fluent_Should_Emit_Directives_When_Applied_Via_Descriptors()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Policy("hasAccess");
                    d.Field("financeReport")
                        .Resolve("report")
                        .Policy([["isAdmin", "isFinance"], ["isOwner"]]);
                    d.Field("auditLog")
                        .Resolve("log")
                        .Policy("isAdmin", PolicyDenialBehavior.Error);
                })
                .AddInterfaceType(d =>
                {
                    d.Name("Secured");
                    d.Policy([["isAdmin", "isFinance"], ["isOwner"]], PolicyDenialBehavior.Abort);
                    d.Field("token").Type<NonNullType<StringType>>().Policy("isOwner");
                })
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query
            }

            type Query @policy(names: "hasAccess") {
              financeReport: String @policy(names: [["isAdmin", "isFinance"], ["isOwner"]])
              auditLog: String @policy(names: "isAdmin", onDenied: ERROR)
            }

            interface Secured
              @policy(names: [["isAdmin", "isFinance"], ["isOwner"]], onDenied: ABORT) {
              token: String! @policy(names: "isOwner")
            }

            "Defines the consequence that applies when a policy expression denies access."
            enum PolicyDenialBehavior {
              "The guarded value is set to null without an error."
              NULL
              "The guarded value is set to null and an authorization error is added."
              ERROR
              "The request is terminated."
              ABORT
            }

            """
            The @policy directive restricts access to the annotated type or field with a policy
            expression in disjunctive normal form. Names within an inner list combine with AND,
            the outer list combines with OR. The expression [["a", "b"], ["c"]] reads as
            (a AND b) OR c. Access is granted only when the expression evaluates to true.


            The onDenied argument defines the consequence when the expression does not evaluate
            to true: NULL sets the guarded value to null without an error, ERROR sets it to null
            and adds an authorization error, ABORT terminates the request.


            Repeated applications on the same member combine with AND and the most severe
            consequence wins.


            directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior! = NULL) repeatable on OBJECT | INTERFACE | FIELD_DEFINITION
            """
            directive @policy(
              "The policy expression in disjunctive normal form. Names within an inner list combine with AND, the outer list combines with OR."
              names: [[String!]!]!
              "The consequence that applies when the policy expression denies access."
              onDenied: PolicyDenialBehavior! = NULL
            ) repeatable on OBJECT | FIELD_DEFINITION | INTERFACE
            """");
    }

    [Fact]
    public static async Task Policy_SchemaFirst_Should_Parse_Directives_When_Declared_In_Sdl()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(SchemaFirstSdl)
                .AddType<Policy>()
                .UseField(next => next)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task Policy_SchemaFirst_Should_Expose_Runtime_Values_When_Declared_In_Sdl()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(SchemaFirstSdl)
                .AddType<Policy>()
                .UseField(next => next)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var queryType = schema.Types.GetType<ObjectType>("Query");
        var typePolicy = queryType.Directives
            .Single(d => d.Name == DirectiveNames.Policy.Name)
            .ToValue<Policy>();
        var financePolicy = queryType.Fields["financeReport"].Directives
            .Single(d => d.Name == DirectiveNames.Policy.Name)
            .ToValue<Policy>();
        var auditPolicy = queryType.Fields["auditLog"].Directives
            .Single(d => d.Name == DirectiveNames.Policy.Name)
            .ToValue<Policy>();
        Assert.Equal("""@policy(names: "hasAccess")""", typePolicy.ToString());
        Assert.Equal(
            """@policy(names: [["isAdmin", "isFinance"], ["isOwner"]], onDenied: ABORT)""",
            financePolicy.ToString());
        Assert.Equal("""@policy(names: [["isAdmin"], ["isFinance"]])""", auditPolicy.ToString());
    }

    [Fact]
    public static async Task Policy_SchemaFirst_Should_Fail_When_Names_Argument_Is_Missing()
    {
        // arrange
        const string sdl =
            """
            type Query {
              field: String @policy
            }
            """;

        // act
        async Task Error() =>
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .AddType<Policy>()
                .UseField(next => next)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(Error);
        Assert.Equal(
            "Cannot parse the @policy directive as it is missing the names argument.",
            exception.Message);
    }

    [Fact]
    public static async Task Policy_SchemaFirst_Should_Fail_When_Names_Argument_Is_Not_String_Or_List()
    {
        // arrange
        const string sdl =
            """
            type Query {
              field: String @policy(names: 123)
            }
            """;

        // act
        async Task Error() =>
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .AddType<Policy>()
                .UseField(next => next)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(Error);
        Assert.Equal(
            "The names argument on @policy must be a string or a list of policy name groups.",
            exception.Message);
    }

    [Fact]
    public static void PolicyAttribute_Should_Throw_When_No_Group_Is_Specified()
    {
        // arrange & act
        var exception = Assert.Throws<ArgumentException>(() => new PolicyAttribute());

        // assert
        Assert.Equal("groups", exception.ParamName);
    }

    [Fact]
    public static void PolicyAttribute_Should_Throw_When_A_Group_Is_Whitespace()
    {
        // arrange & act
        var exception = Assert.Throws<ArgumentException>(() => new PolicyAttribute("isAdmin", " "));

        // assert
        Assert.Equal("groups", exception.ParamName);
    }

    [Fact]
    public static void Policy_Constructor_Should_Throw_When_No_Group_Is_Specified()
    {
        // arrange & act
        var exception = Assert.Throws<ArgumentException>(() => new Policy([]));

        // assert
        Assert.Equal("names", exception.ParamName);
    }

    [Fact]
    public static void Policy_Constructor_Should_Throw_When_A_Group_Is_Empty()
    {
        // arrange & act
        var exception = Assert.Throws<ArgumentException>(() => new Policy([[]]));

        // assert
        Assert.Equal("names", exception.ParamName);
    }

    [Fact]
    public static void Policy_Constructor_Should_Throw_When_A_Group_Is_Null()
    {
        // arrange & act
        var exception = Assert.Throws<ArgumentException>(() => new Policy([null!]));

        // assert
        Assert.Equal("names", exception.ParamName);
    }

    [Fact]
    public static void Policy_Constructor_Should_Throw_When_A_Name_Is_Whitespace()
    {
        // arrange & act
        var exception = Assert.Throws<ArgumentException>(() => new Policy([["isAdmin", " "]]));

        // assert
        Assert.Equal("names", exception.ParamName);
    }

    [Fact]
    public static void Policy_Constructor_Should_Throw_When_A_Name_Has_Trailing_Whitespace()
    {
        // arrange & act
        var exception = Assert.Throws<ArgumentException>(() => new Policy([["isAdmin "]]));

        // assert
        Assert.Equal(
            "A policy name must not have leading or trailing whitespace. (Parameter 'names')",
            exception.Message);
    }

    [Fact]
    public static void Policy_Constructor_Should_Throw_When_Single_Name_Is_Whitespace()
    {
        // arrange & act
        var exception = Assert.Throws<ArgumentException>(() => new Policy(" "));

        // assert
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public static void Policy_Constructor_Should_Throw_When_Single_Name_Has_Leading_Whitespace()
    {
        // arrange & act
        var exception = Assert.Throws<ArgumentException>(() => new Policy(" isAdmin"));

        // assert
        Assert.Equal(
            "A policy name must not have leading or trailing whitespace. (Parameter 'name')",
            exception.Message);
    }

    [Fact]
    public static void Policy_Constructor_Should_Allow_Name_With_Interior_Whitespace()
    {
        // arrange & act
        var policy = new Policy("has access");

        // assert
        Assert.Equal("""@policy(names: "has access")""", policy.ToString());
    }

    [Fact]
    public static async Task PolicyAttribute_Should_Throw_When_Applied_To_Input_Object_Type()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("stub").Resolve("stub"))
            .AddInputObjectType<GuardedInput>();

        // act
        async Task Error() =>
            await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        // the SchemaException from the attribute is wrapped by the type registrar
        var exception = await Assert.ThrowsAsync<SchemaException>(Error);
        var error = Assert.Single(exception.Errors);
        var inner = Assert.IsType<SchemaException>(error.Exception);
        var innerError = Assert.Single(inner.Errors);
        Assert.Equal(
            "Policy directive is only supported on object types, interface "
            + "types, and field definitions.",
            innerError.Message);
    }

    private const string SchemaFirstSdl =
        """
        type Query @policy(names: "hasAccess") {
          financeReport: String!
            @policy(names: [["isAdmin", "isFinance"], ["isOwner"]], onDenied: ABORT)
          auditLog: String! @policy(names: ["isAdmin", "isFinance"])
        }
        """;

    [Policy("hasAccess")]
    public class GuardedInput
    {
        public string? Value { get; set; }
    }

    [Policy("hasAccess")]
    public class Query
    {
        [Policy("isAdmin isFinance", "isOwner")]
        public string GetFinanceReport() => "report";

        [Policy("isAdmin", OnDenied = PolicyDenialBehavior.Error)]
        public string GetAuditLog() => "log";

        [Policy("isAdmin")]
        [Policy("isFinance", OnDenied = PolicyDenialBehavior.Abort)]
        public string GetSalary() => "salary";
    }

    public class QueryWithInterface
    {
        public ISecured GetSecured() => new Document();
    }

    [InterfaceType("Secured")]
    [Policy("hasAccess")]
    public interface ISecured
    {
        [Policy("isAdmin isFinance", "isOwner", OnDenied = PolicyDenialBehavior.Abort)]
        string Token { get; }
    }

    public class Document : ISecured
    {
        public string Token => "token";
    }
}
