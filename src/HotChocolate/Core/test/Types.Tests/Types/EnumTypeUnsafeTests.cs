using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Types
{
    public class EnumTypeUnsafeTests
    {
        [Fact]
        public async Task Create_Enum_Unsafe_With_Two_Values()
        {
            // arrange
            // act
            var enumType = EnumType.CreateUnsafe(
                new("Simple")
                {
                    Values =
                    {
                        new("ONE", runtimeValue: "One"),
                        new("TWO", runtimeValue: "Two")
                    }
                });

            // assert
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("foo").Type(enumType).Resolve("One");
                })
                .BuildSchemaAsync()
                .MatchSnapshotAsync();
        }

    }
}
