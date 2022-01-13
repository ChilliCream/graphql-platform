using Xunit;

#nullable enable

namespace HotChocolate.Types
{
    public class ReservedTypeNameTests
    {
        [InlineData("_Any")]
        [InlineData("_FieldSet")]
        [Theory]
        public void Ensure_that_our_reserved_string_types_are_strings(string stringTypeName)
        {
            Assert.IsType<StringType>(
                SchemaBuilder.New()
                    .AddQueryType(d => d
                        .Name("Query")
                        .Field("a")
                        .Argument("a", a => a.Type(stringTypeName))
                        .Type("String")
                        .Resolve(ctx => ctx.ArgumentValue<object>("a")))
                    .AddType(new StringType())
                    .AddType(new AnyType(stringTypeName))
                    .Create()
                    .GetType<ScalarType>(stringTypeName));
        }
    }
}
