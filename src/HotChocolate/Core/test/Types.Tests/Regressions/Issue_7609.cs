using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Regressions;

public class Issue_7609
{
    [Fact]
    public async Task Query_Only_Reusing_Dtos_Works()
    {
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Issue7609Query>()
            .AddType<Issue7609Boat>()
            .AddType<Issue7609Car>()
            .BuildSchemaAsync();

        Assert.NotNull(schema);
    }

    [Fact]
    public async Task Interface_Output_And_OneOf_Input_Reusing_Dtos_Does_Not_Throw()
    {
        // Repro from #7609: reuse the same DTOs for interface output and oneOf input.
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Issue7609Query>()
            .AddMutationType<Issue7609Mutation>()
            .AddType<Issue7609Boat>()
            .AddType<Issue7609Car>()
            .AddType<Issue7609BoatInput>()
            .AddType<Issue7609CarInput>()
            .BuildSchemaAsync();

        Assert.NotNull(schema);
    }

    public class Issue7609Query
    {
        public Issue7609BaseThing? GetThing(string? name) => name switch
        {
            "Car" => new Issue7609Car { Name = name, Make = "Toyota" },
            "Boat" => new Issue7609Boat { Name = name, Make = "Yamaha", Length = 10 },
            _ => null
        };
    }

    [InterfaceType]
    public interface Issue7609BaseThing
    {
        string? Name { get; }
    }

    [ObjectType]
    public class Issue7609Car : Issue7609BaseThing
    {
        public required string Name { get; set; }

        public required string Make { get; set; }
    }

    [ObjectType]
    public class Issue7609Boat : Issue7609BaseThing
    {
        public required string Name { get; set; }

        public required string Make { get; set; }

        public int Length { get; set; }
    }

    public class Issue7609Mutation
    {
        public bool AddThingy(Issue7609CouldBeInputThingInput thing) => thing is not null;
    }

    [OneOf]
    public class Issue7609CouldBeInputThingInput
    {
        public Issue7609Boat? Boat { get; set; }

        public Issue7609Car? Car { get; set; }
    }

    public class Issue7609BoatInput : InputObjectType<Issue7609Boat>;

    public class Issue7609CarInput : InputObjectType<Issue7609Car>;
}
