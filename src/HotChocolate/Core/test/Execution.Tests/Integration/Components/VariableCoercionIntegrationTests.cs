using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Integration.Components;

public class VariableCoercionIntegrationTests
{
    [Fact]
    public async Task Nullables_And_NonNullables_Are_Set()
    {
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    mutation($user: UserInput!) {
                        addUser(user: $user)
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                        "user": {
                            "name": "Oliver",
                            "surname": "Smith",
                            "gender": "MALE"
                        }
                    }
                    """)
                .Build();

        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Nullables_Are_Not_Set_NonNullables_Are_Set()
    {
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    mutation($user: UserInput!) {
                        addUser(user: $user)
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                        "user": {
                            "name": "Oliver"
                        }
                    }
                    """)
                .Build();

        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Nullables_Are_Set_And_NonNullables_Are_Set_To_Null()
    {
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    mutation($user: UserInput!) {
                        addUser(user: $user)
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                        "user": {
                            "name": null,
                            "surname": "Smith",
                            "gender": "MALE"
                        }
                    }
                    """)
                .Build();

        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Nullables_Are_Set_And_NonNullables_Not_Are_Set()
    {
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    mutation($user: UserInput!) {
                        addUser(user: $user)
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                        "user": {
                            "surname": "Smith",
                            "gender": "MALE"
                        }
                    }
                    """)
                .Build();

        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Empty_Object()
    {
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    mutation($user: UserInput!) {
                        addUser(user: $user)
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                        "user": {}
                    }
                    """)
                .Build();

        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Variable_Null()
    {
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    mutation($user: UserInput!) {
                        addUser(user: $user)
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                        "user": null
                    }
                    """)
                .Build();

        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Variable_Not_Provided()
    {
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    mutation($user: UserInput!) {
                        addUser(user: $user)
                    }
                    """)
                .Build();

        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Invalid_Field_Provided()
    {
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    mutation($user: UserInput!) {
                        addUser(user: $user)
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                        "user": {
                            "name": "Oliver",
                            "surname": "Smith",
                            "foo": "bar"
                        }
                    }
                    """)
                .Build();

        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Invalid_Field_Provided_When_Enum_Is_Present()
    {
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    mutation($user: UserInput!) {
                        addUser(user: $user)
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                        "user": {
                            "name": "Oliver",
                            "surname": "Smith",
                            "gender": "MALE",
                            "foo": "bar"
                        }
                    }
                    """)
                .Build();

        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Invalid_Enum_Value_Provided()
    {
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    mutation($user: UserInput!) {
                        addUser(user: $user)
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                        "user": {
                            "name": "Oliver",
                            "gender": "FOO"
                        }
                    }
                    """)
                .Build();

        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken).MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Report_Input_Path_Without_Path_When_Mutation_Input_Object_Variable_Has_Invalid_Leaf()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    mutation($input: UpdateUserInput!) {
                        updateUser(input: $input)
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                        "input": {
                            "avatar": ""
                        }
                    }
                    """)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The value is not a valid image data URL.",
                  "extensions": {
                    "inputPath": [
                      "input",
                      "avatar"
                    ],
                    "coordinate": "UpdateUserInput.avatar",
                    "fieldType": "ImageDataUrl"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Report_Input_Path_Without_Path_When_Mutation_Scalar_Variable_Is_Invalid()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    mutation($avatar: ImageDataUrl) {
                        updateUser(input: { avatar: $avatar })
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                        "avatar": ""
                    }
                    """)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The value is not a valid image data URL.",
                  "extensions": {
                    "inputPath": [
                      "avatar"
                    ],
                    "fieldType": "ImageDataUrl"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Report_Input_Path_Without_Path_When_Query_Input_Object_Variable_Has_Invalid_Leaf()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query($input: UpdateUserInput!) {
                        avatarPreview(input: $input)
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                        "input": {
                            "avatar": ""
                        }
                    }
                    """)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The value is not a valid image data URL.",
                  "extensions": {
                    "inputPath": [
                      "input",
                      "avatar"
                    ],
                    "coordinate": "UpdateUserInput.avatar",
                    "fieldType": "ImageDataUrl"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Report_Input_Path_Without_Path_When_Query_Scalar_Variable_Is_Invalid()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query($image: ImageDataUrl) {
                        avatarPreview(input: { avatar: $image })
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                        "image": ""
                    }
                    """)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The value is not a valid image data URL.",
                  "extensions": {
                    "inputPath": [
                      "image"
                    ],
                    "fieldType": "ImageDataUrl"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Report_Input_Path_Without_Path_When_Query_Scalar_Variable_Default_Is_Invalid()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query($image: ImageDataUrl = "") {
                        avatarPreview(input: { avatar: $image })
                    }
                    """)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The value is not a valid image data URL.",
                  "extensions": {
                    "inputPath": [
                      "image"
                    ],
                    "fieldType": "ImageDataUrl"
                  }
                }
              ]
            }
            """);
    }

    private static async Task<IRequestExecutor> CreateSchemaAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                t =>
                {
                    t.Field("a").Resolve("b");

                    t.Field("avatarPreview")
                        .Argument("input", a => a.Type<NonNullType<InputObjectType<UpdateUserInput>>>())
                        .Type<StringType>()
                        .Resolve(ctx => ctx.ArgumentValue<UpdateUserInput>("input").Avatar);
                })
            .AddMutationType<UserMutationType>()
            .BuildRequestExecutorAsync();
    }

    public class UserMutation
    {
        public string AddUser(User user)
        {
            return $"{user.Name} {user.Surname} ({user.Gender}) was added!";
        }

        public string UpdateUser(UpdateUserInput input)
        {
            return $"{input.Avatar} was updated!";
        }
    }

    public class UserMutationType : ObjectType<UserMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<UserMutation> descriptor)
        {
            descriptor
                .Field(um => um.AddUser(new User("Oliver")))
                .Description("Add user to db")
                .Argument("user", d => d.Type<NonNullType<UserInputType>>()
                    .Description("User input type, required"));

            descriptor
                .Field(um => um.UpdateUser(default!))
                .Argument("input", d => d.Type<NonNullType<InputObjectType<UpdateUserInput>>>());
        }
    }

    public class User(string name)
    {
        public string Name { get; set; } = name;

        public string? Surname { get; set; }

        public GenderEnum? Gender { get; set; }
    }

    public enum GenderEnum { Male, Female }

    public class UserInputType : InputObjectType<User>
    {
        protected override void Configure(IInputObjectTypeDescriptor<User> descriptor)
        {
            descriptor
                .Field(f => f.Name)
                .Type<NonNullType<StringType>>()
                .Description("User's name, required");

            descriptor.Field(f => f.Surname)
                .Description("User's surname");
        }
    }
}
