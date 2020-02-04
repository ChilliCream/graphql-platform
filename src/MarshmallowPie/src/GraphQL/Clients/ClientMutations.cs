using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using MarshmallowPie.GraphQL.Schemas;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Clients
{

    [ExtendObjectType(Name = "Mutation")]
    public class ClientMutations
    {
        public async Task<CreateClientPayload> CreateClientAsync(
            CreateClientInput input,
            [Service]IClientRepository repository,
            [Service]IIdSerializer idSerializer,
            [DataLoader]SchemaByIdDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            IdValue deserializedId = idSerializer.Deserialize(input.SchemaId);

            if (!deserializedId.TypeName.Equals(nameof(Schema), StringComparison.Ordinal))
            {
                throw new GraphQLException("The specified id type is invalid.");
            }

            Schema schema = await dataLoader.LoadAsync(
                (Guid)deserializedId.Value, cancellationToken)
                .ConfigureAwait(false);

            var client = new Client(schema.Id, input.Name, input.Description);

            await repository.AddClientAsync(
                client, cancellationToken)
                .ConfigureAwait(false);

            return new CreateClientPayload(schema, client, input.ClientMutationId);
        }

        /*
    public async Task<UpdateSchemaPayload> UpdateSchema(
        UpdateSchemaInput input,
        [Service]IIdSerializer idSerializer,
        [Service]ISchemaRepository repository,
        CancellationToken cancellationToken)
    {
        IdValue deserializedId = idSerializer.Deserialize(input.Id);

        if (!deserializedId.TypeName.Equals(nameof(Schema), StringComparison.Ordinal))
        {
            throw new GraphQLException(Resources.General_IdTypeInvalid);
        }

        var schema = new Schema(
            (Guid)deserializedId.Value,
            input.Name,
            input.Description);

        await repository.UpdateSchemaAsync(schema, cancellationToken)
            .ConfigureAwait(false);

        return new UpdateSchemaPayload(schema, input.ClientMutationId);
    }

    public async Task<PublishSchemaPayload> PublishSchemaAsync(
        PublishSchemaInput input,
        [Service]IMessageSender<PublishDocumentMessage> messageSender,
        [Service]IFileStorage fileStorage,
        [Service]ISessionCreator sessionCreator,
        [DataLoader]SchemaByNameDataLoader schemaDataLoader,
        [DataLoader]EnvironmentByNameDataLoader environmentDataLoader,
        CancellationToken cancellationToken)
    {
        Schema schema = await schemaDataLoader.LoadAsync(
            input.SchemaName, cancellationToken)
            .ConfigureAwait(false);

        Environment environment = await environmentDataLoader.LoadAsync(
            input.EnvironmentName, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(input.SourceText))
        {
            throw new GraphQLException(
                Resources.SchemaMutations_HashAndSourceTextAreNull);
        }

        string sessionId = await sessionCreator.CreateSessionAsync(cancellationToken)
            .ConfigureAwait(false);

        IFileContainer container = await fileStorage.CreateContainerAsync(
            sessionId, cancellationToken)
            .ConfigureAwait(false);

        using (Stream stream = await container.CreateFileAsync(
            "schema.graphql", cancellationToken)
            .ConfigureAwait(false))
        {
            byte[] buffer = Encoding.UTF8.GetBytes(input.SourceText);
            await stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
        }

        await messageSender.SendAsync(
            new PublishDocumentMessage(
                sessionId,
                environment.Id,
                schema.Id,
                input.ExternalId,
                input.Tags is null
                    ? Array.Empty<Tag>()
                    : input.Tags.Select(t => new Tag(t.Key, t.Value)).ToArray()),
            cancellationToken)
            .ConfigureAwait(false);

        return new PublishSchemaPayload(sessionId, input.ClientMutationId);
    }
    */
    }
}
