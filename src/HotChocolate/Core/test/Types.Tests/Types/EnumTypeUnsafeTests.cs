using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

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
                    new("TWO", runtimeValue: "Two"),
                },
            });

        var queryType = ObjectType.CreateUnsafe(
            new("Query")
            {
                Fields =
                {
                    new("foo", type: TypeReference.Create(enumType), pureResolver: _ => "One"),
                },
            });

        // assert
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(queryType)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Create_Enum_Unsafe_With_Descriptor()
    {
        // arrange
        // act
        var enumType = EnumType.CreateUnsafe(
            new EnumTypeDefinition("Simple")
            {
                Values =
                {
                    new("ONE", runtimeValue: "One"),
                    new("TWO", runtimeValue: "Two"),
                },
            });

        var queryType = ObjectType.CreateUnsafe(
            new("Query")
            {
                Fields =
                {
                    new("foo", type: TypeReference.Create(enumType), pureResolver: _ => "One"),
                },
            });

        // assert
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(queryType)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }
}
