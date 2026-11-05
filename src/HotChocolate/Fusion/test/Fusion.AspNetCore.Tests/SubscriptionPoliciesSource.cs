using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion;

/// <summary>
/// The source schema behind the subscription authorization transport tests. Its subscription
/// fields carry the authorization directives of the gateway and stream the events of a
/// <see cref="SubscriptionPoliciesFeed"/>.
/// </summary>
public static class SubscriptionPoliciesSource
{
    public const string LivePolicy = "live";
    public const string FlipPolicy = "flip";

    public static void Configure(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType<Query>()
            .AddSubscriptionType<SubscriptionType>()
            .AddType<ItemType>()
            .AddDirectiveType(CreateDirective("authenticated", argumentName: null))
            .AddDirectiveType(CreateDirective("requiresScopes", "scopes"))
            .AddDirectiveType(CreateDirective("policy", "policies"));

    public sealed record Item(string Id, string Name, string Code, string Note, string Tag);

    public sealed class Query
    {
        public int? Open => 1;
    }

    public sealed class Subscription
    {
        public IAsyncEnumerable<Item> Stream(
            [Service] SubscriptionPoliciesFeed feed,
            CancellationToken cancellationToken)
            => feed.SubscribeAsync(cancellationToken);

        [Subscribe(With = nameof(Stream))]
        public Item? Changed([EventMessage] Item item) => item;

        [Subscribe(With = nameof(Stream))]
        public Item Required([EventMessage] Item item) => item;

        [Subscribe(With = nameof(Stream))]
        public Item? Secret([EventMessage] Item item) => item;

        [Subscribe(With = nameof(Stream))]
        public Item? Scoped([EventMessage] Item item) => item;
    }

    public sealed class SubscriptionType : ObjectType<Subscription>
    {
        protected override void Configure(IObjectTypeDescriptor<Subscription> descriptor)
        {
            descriptor.Name("Subscription");
            descriptor.Field(t => t.Secret(default!)).Directive("authenticated");
            descriptor
                .Field(t => t.Scoped(default!))
                .Directive("requiresScopes", Matrix("scopes", "read"));
        }
    }

    public sealed class ItemType : ObjectType<Item>
    {
        protected override void Configure(IObjectTypeDescriptor<Item> descriptor)
        {
            descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
            descriptor.Field(t => t.Name).Type<StringType>().Directive("policy", Matrix("policies", LivePolicy));
            descriptor.Field(t => t.Code).Type<NonNullType<StringType>>()
                .Directive("policy", Matrix("policies", LivePolicy));
            descriptor.Field(t => t.Note).Type<StringType>().Directive("policy", Matrix("policies", FlipPolicy));
            descriptor.Field(t => t.Tag).Type<StringType>();
        }
    }

    private static DirectiveType CreateDirective(string name, string? argumentName)
        => new(
            descriptor =>
            {
                descriptor
                    .Name(name)
                    .Location(
                        DirectiveLocation.FieldDefinition
                        | DirectiveLocation.Object
                        | DirectiveLocation.Interface
                        | DirectiveLocation.Enum
                        | DirectiveLocation.Scalar);

                if (argumentName is not null)
                {
                    descriptor
                        .Argument(argumentName)
                        .Type<NonNullType<ListType<NonNullType<ListType<NonNullType<StringType>>>>>>();
                }
            });

    private static ArgumentNode Matrix(string argument, string name)
        => new(
            argument,
            new ListValueNode(new ListValueNode(new StringValueNode(name))));
}
