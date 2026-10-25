namespace HotChocolate.Execution.Integration;

public class UpdateUserInput
{
    [GraphQLType<ImageDataUrlType>]
    public string? Avatar { get; set; }
}
