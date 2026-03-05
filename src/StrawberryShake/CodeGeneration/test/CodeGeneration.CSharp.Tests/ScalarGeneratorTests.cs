using ChilliCream.Testing;
using Xunit.Sdk;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp;

public class ScalarGeneratorTests
{
    [Fact]
    public void Simple_Custom_Scalar() =>
        AssertResult(
            "query GetPerson { person { name email } }",
            "type Query { person: Person }",
            "type Person { name: String! email: Email }",
            "scalar Email",
            "extend schema @key(fields: \"id\")");

    [Fact]
    public void Base64String_ScalarType() =>
        AssertResult(
            "query GetAttachment { base64String }",
            "type Query { base64String: Base64String! }",
            "scalar Base64String",
            "extend schema @key(fields: \"id\")");

    [Fact]
    public void ByteArray_ScalarType() =>
        AssertResult(
            "query GetAttachment { byteArray }",
            "type Query { byteArray: ByteArray! }",
            "scalar ByteArray",
            "extend schema @key(fields: \"id\")");

    [Fact]
    public void Only_Custom_Scalars() =>
        AssertResult(
            "query GetPerson { person { email } }",
            "type Query { person: Person }",
            "type Person { email: Email }",
            "scalar Email",
            "extend schema @key(fields: \"id\")");

    [Fact]
    public void Any_Type() =>
        AssertResult(
            "query GetPerson { person { name email } }",
            "type Query { person: Person }",
            "type Person { name: String! email: Any }",
            "scalar Any",
            "extend schema @key(fields: \"id\")");

    [Fact]
    public void Custom_Scalar_With_RuntimeType() =>
        AssertResult(
            "query GetPerson { person { name email } }",
            "type Query { person: Person }",
            "type Person { name: String! email: Email }",
            "scalar Email",
            "extend scalar Email @runtimeType(name: \"global::System.Int32\")",
            "extend schema @key(fields: \"id\")");

    [Fact]
    public void Custom_Scalar_With_RuntimeType_ValueType_AsInput() =>
        // Using System.Index here because it exists and is not part of the default TypeInfos
        AssertResult(
            "query SetPerson($email: Email!) { person { setEmail(email: $email) } }",
            "type Query { person: Person }",
            "type Person { setEmail(email: Email!): Email! }",
            "scalar Email",
            "extend scalar Email @runtimeType(name: \"global::System.Index\", valueType: true)");

    [Fact]
    public void Custom_Scalar_With_Unknown_RuntimeType() =>
        AssertResult(
            "query GetPerson { person { name email } }",
            "type Query { person: Person }",
            "type Person { name: String! email: Email }",
            "scalar Email",
            """
            extend scalar Email @runtimeType(
                name: "global::StrawberryShake.CodeGeneration.CSharp.Custom")
            """,
            "extend schema @key(fields: \"id\")");

    [Fact]
    public void Custom_Scalar_With_SerializationType() =>
        AssertResult(
            "query GetPerson { person { name email } }",
            "type Query { person: Person }",
            "type Person { name: String! email: Email }",
            "scalar Email",
            "extend scalar Email @serializationType(name: \"global::System.Int32\")",
            "extend schema @key(fields: \"id\")");

    [Fact]
    public void Custom_Scalar_With_SerializationType_And_RuntimeType() =>
        AssertResult(
            "query GetPerson { person { name email } }",
            "type Query { person: Person }",
            "type Person { name: String! email: Email }",
            "scalar Email",
            """
            extend scalar Email
                @runtimeType(name: "global::System.Int64")
                @serializationType(name: "global::System.Int32")
            """,
            "extend schema @key(fields: \"id\")");

    [Fact]
    public void Custom_Scalar_With_ValueType_RuntimeType() =>
        AssertResult(
            "query GetId($modelId: ModelIdScalar!) { personId(id: $modelId) }",
            "type Query { personId(id: ModelIdScalar!): ModelIdScalar }",
            "scalar ModelIdScalar",
            """
            extend scalar ModelIdScalar
                @runtimeType(name: "global::StrawberryShake.CodeGeneration.CSharp.ModelId" valueType: true)
            """);

    [Fact]
    public void Custom_Scalar_With_ValueType_RuntimeType_Used_As_Nullable_Input() =>
        AssertResult(
            "query GetId($modelId: ModelIdScalar) { personId(id: $modelId) }",
            "type Query { personId(id: ModelIdScalar): ModelIdScalar }",
            "scalar ModelIdScalar",
            """
            extend scalar ModelIdScalar
                @runtimeType(name: "global::StrawberryShake.CodeGeneration.CSharp.ModelId" valueType: true)
            """);

    [Fact]
    public void Custom_Scalar_With_ValueType_RuntimeType_Fails_If_ValueType_Not_Specified() =>
        Assert.Throws<FailException>(() =>
            AssertResult(
            "query GetId($modelId: ModelIdScalar!) { personId(id: $modelId) }",
            "type Query { personId(id: ModelIdScalar!): ModelIdScalar }",
            "scalar ModelIdScalar",
            """
            extend scalar ModelIdScalar
                @runtimeType(name: "global::StrawberryShake.CodeGeneration.CSharp.ModelId")
            """));

    [Fact]
    public void Any_Scalar() =>
        AssertResult(
            "query GetPerson { person { name data } }",
            "type Query { person: Person }",
            "type Person { name: String! data: Any }",
            "scalar Any",
            """
            extend scalar Any
                @runtimeType(name: "global::System.Object")
                @serializationType(name: "global::System.Text.Json.JsonElement")
            """,
            "extend schema @key(fields: \"id\")");

    [Fact]
    public void Complete_Schema_With_UUID_And_DateTime()
    {
        AssertResult(
            FileResource.Open("AllExpenses.graphql"),
            FileResource.Open("Expenses.extensions.graphql"),
            FileResource.Open("Expenses.graphql"));
    }

    [Fact]
    public void TimeSpan_Not_Detected()
    {
        AssertResult(
            strictValidation: false,
            @"
                    query GetSessions {
                      sessions(order: { title: ASC }) {
                        nodes {
                          title
                        }
                      }
                    }
                ",
            FileResource.Open("Workshop.Schema.graphql"),
            "extend schema @key(fields: \"id\")");
    }

    [Fact]
    public void Scalars_Are_Correctly_Inferred()
    {
        AssertResult(
            @"
                query getAll {
                  listings {
                    ...Offer
                  }
                }
                fragment Offer on Offer {
                   numberFloat
                   numberInt
                }",
            @"
                schema {
                  query: Query
                  mutation: null
                  subscription: null
                }
                type Query {
                  listings: [Offer!]!
                }
                type Offer{
                  listingId: ID!
                  numberInt: Int
                  numberFloat: Float
                }",
            "extend schema @key(fields: \"id\")");
    }

    [Fact]
    public void Uuid_Type() =>
        AssertResult(
            "query GetPerson { person { Uuid UUID } }",
            "type Query { person: Person }",
            "type Person { Uuid:Uuid UUID:UUID }",
            "scalar UUID",
            "scalar Uuid",
            "extend schema @key(fields: \"id\")");

    [Fact]
    public void Uri_Type() =>
        AssertResult(
            "query GetPerson { person { uri URI } }",
            "type Query { person: Person }",
            "type Person { uri:Uri URI:URI }",
            "scalar Uri",
            "scalar URI",
            "extend schema @key(fields: \"id\")");
}

public class Custom;

public record struct ModelId(long Id);
