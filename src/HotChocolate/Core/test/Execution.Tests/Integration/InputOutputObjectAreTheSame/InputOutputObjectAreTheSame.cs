using HotChocolate.Types;

namespace HotChocolate.Execution.Integration.InputOutputObjectAreTheSame;

public class InputOutputObjectAreTheSame
{
    [Fact]
    public void CheckIfTypesAreRegisteredCorrectly()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var containsPersonInputType = schema.TryGetType<INamedInputType>("PersonInput", out _);
        var containsPersonOutputType = schema.TryGetType<INamedOutputType>("Person", out _);

        // assert
        Assert.True(containsPersonInputType);
        Assert.True(containsPersonOutputType);
    }

    [Fact]
    public async Task ExecuteQueryThatReturnsPerson()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync(@"{
                    person(person: { firstName:""a"", lastName:""b"" }) {
                        lastName
                        firstName
                    }
                }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    private static ISchema CreateSchema()
        => SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType<ObjectType<Person>>()
            .AddType(new InputObjectType<Person>(
                d => d.Name("PersonInput")))
            .Create();
}
