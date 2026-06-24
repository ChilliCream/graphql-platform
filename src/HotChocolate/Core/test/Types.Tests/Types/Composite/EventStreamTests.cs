using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class EventStreamTests
{
    [Fact]
    public static async Task EventStream_Attribute_And_Fluent_Should_Emit_Directives()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddSubscriptionType<SubscriptionsType>()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              subscription: Subscriptions
            }

            type Subscriptions {
              onUserDeleted(after: String! @eventCursor, id: String!): OnUserCreatedEvent!
                @subscribe(message: "user { id }", topics: ["onUserDeleted"], broker: "bar")
              onUserCreated(after: String! @eventCursor, id: String!): OnUserCreatedEvent!
                @subscribe(
                  message: "user { id }"
                  topics: ["onUserCreated-{$args.id}"]
                  broker: "foo"
                )
            }

            type OnUserCreatedEvent {
              user: User!
              cursor: String! @eventCursor
            }

            type User {
              name: String!
              age: Int!
            }

            scalar FieldSelectionSet

            """
            The @eventCursor directive marks the cursor of an event stream. On a subscription
            field argument it marks the resume input that the distributed executor uses to
            continue a stream after a previously received event. On an output field it marks
            the value that carries the cursor of each emitted event, which a client can store
            and later pass back to resume the stream.


            directive @eventCursor on ARGUMENT_DEFINITION | FIELD_DEFINITION
            """
            directive @eventCursor on FIELD_DEFINITION | ARGUMENT_DEFINITION

            """
            The @subscribe directive declares that a subscription field is fulfilled by an
            event stream behind the distributed GraphQL executor. The directive carries the
            payload selection set as well as the topics and broker that the executor uses to
            resolve the stream.


            directive @subscribe(message: FieldSelectionSet!, topics: [String!], broker: String) on FIELD_DEFINITION
            """
            directive @subscribe(
              "Gets the payload selection set."
              message: FieldSelectionSet!
              "Gets the topics the event stream subscribes to."
              topics: [String!]
              "Gets the broker that provides the event stream."
              broker: String
            ) on FIELD_DEFINITION
            """");
    }

    // fluent authoring of the @subscribe and @eventCursor directives
    public class SubscriptionsType : ObjectType<Subscriptions>
    {
        protected override void Configure(IObjectTypeDescriptor<Subscriptions> descriptor)
        {
            descriptor
                .Field(f => f.OnUserDeleted(default!, default!))
                .EventStream("user { id }", "onUserDeleted", "bar")
                .Argument("after", a => a.EventCursor());
        }
    }

    public class Subscriptions
    {
        // attribute authoring of the @subscribe and @eventCursor directives
        [EventStream("user { id }", Topic = "onUserCreated-{$args.id}", Broker = "foo")]
        public OnUserCreatedEvent OnUserCreated([EventCursor] string after, string id)
            => EventStream.Create<OnUserCreatedEvent>(after);

        public OnUserCreatedEvent OnUserDeleted(string after, string id)
            => EventStream.Create<OnUserCreatedEvent>(after);
    }

    public record OnUserCreatedEvent(User User, [property: EventCursor] string Cursor);

    public record User(string Name, int Age);
}
