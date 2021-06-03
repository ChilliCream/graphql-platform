using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EntityIdFactoryGeneratorTests
    {
        [Fact]
        public void Simple_NoEntity() =>
            AssertResult(
                "query GetPerson { person { name email } }",
                "type Query { person: Person }",
                "type Person { name: String! email: String }",
                "extend schema @key(fields: \"id\")");

        [Fact]
        public void Simple_IdEntity() =>
            AssertResult(
                "query GetPerson { person { id email } }",
                "type Query { person: Person }",
                "type Person { id: String! email: String }",
                "extend schema @key(fields: \"id\")");

        [Fact]
        public void Simple_ComplexEntity() =>
            AssertResult(
                "query GetPerson { person { id email } }",
                "type Query { person: Person }",
                "type Person { id: String! email: String }",
                "extend schema @key(fields: \"id email\")");

        [Fact]
        public void Simple_Uuid_Entity() =>
            AssertResult(
                "query GetPerson { person { id email } }",
                "type Query { person: Person }",
                "type Person { id: Uuid! email: String }",
                "extend schema @key(fields: \"id\")");

        [Fact]
        public void Simple_DateTimeOffset_Entity() =>
            AssertResult(
                "query GetPerson { person { id email } }",
                "type Query { person: Person }",
                "type Person { id: DateTime! email: String }",
                "extend schema @key(fields: \"id\")");
    }
}
