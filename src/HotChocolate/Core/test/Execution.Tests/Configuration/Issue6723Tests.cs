using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class Issue6723Tests
{
    [Fact]
    public async Task TypeModule_FieldLevelMutationConvention_WithError_ShouldBuildSchema()
    {
        // arrange
        var services = new ServiceCollection()
            .AddSingleton<Issue6723TypeModule>()
            .AddGraphQLServer()
            .AddMutationConventions()
            .AddTypeModule<Issue6723TypeModule>()
            .Services
            .BuildServiceProvider();

        // act
        var schema = await services.GetSchemaAsync();

        // assert
        var mutationField = schema.MutationType!.Fields["api_accessGroups"];
        Assert.Equal("ApiAccessGroupsPayload", mutationField.Type.NamedType().Name);

        var payloadType = schema.Types.GetType<ObjectType>("ApiAccessGroupsPayload");
        var errorsField = payloadType.Fields["errors"];
        Assert.Equal("ApiAccessGroupsError", errorsField.Type.NamedType().Name);

        var errorUnion = schema.Types.GetType<UnionType>("ApiAccessGroupsError");
        Assert.Contains(errorUnion.Types, t => t.Name == "Issue6723MutationError");
    }

    private sealed class Issue6723TypeModule : ITypeModule
    {
#pragma warning disable CS0067
        public event EventHandler<EventArgs>? TypesChanged;
#pragma warning restore CS0067

        public ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
            IDescriptorContext context,
            CancellationToken cancellationToken)
        {
            var members = new List<ITypeSystemMember>();

            var query = new ObjectTypeConfiguration("Query");
            query.Fields.Add(
                new ObjectFieldConfiguration(
                    "ping",
                    type: TypeReference.Parse("String!"),
                    pureResolver: _ => "pong"));
            members.Add(ObjectType.CreateUnsafe(query));

            var mutation = new ObjectTypeConfiguration("Mutation");
            var mutationField = new ObjectFieldConfiguration(
                "api_accessGroups",
                description: "Mutation for issue 6723",
                type: TypeReference.Parse("[String!]!"),
                pureResolver: _ => Array.Empty<string>());

            mutationField.Arguments.Add(
                new InputFieldConfiguration(
                    "entities",
                    type: TypeReference.Parse("[String!]")));

            mutationField = mutationField
                .ToDescriptor(context)
                .UseMutationConvention(
                    new MutationFieldOptions
                    {
                        Disable = false,
                        PayloadFieldName = "data",
                        InputTypeName = "ApiAccessGroupsInput",
                        PayloadTypeName = "ApiAccessGroupsPayload",
                        PayloadErrorTypeName = "ApiAccessGroupsError"
                    })
                .Error<Issue6723MutationError>()
                .ToConfiguration();

            mutation.Fields.Add(mutationField);
            members.Add(ObjectType.CreateUnsafe(mutation));

            return new(members);
        }
    }

    private sealed class Issue6723MutationError
    {
        public string Message => "error";
    }
}
