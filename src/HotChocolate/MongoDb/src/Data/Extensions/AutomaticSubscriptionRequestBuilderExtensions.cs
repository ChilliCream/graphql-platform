using HotChocolate.Data.MongoDb.Subscriptions;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Extensions;

public static class AutomaticSubscriptionRequestBuilderExtensions
{
    public static IRequestExecutorBuilder AddCreateSubscriptions<T>(this IRequestExecutorBuilder builder)
    {
        var fieldName = $"on{typeof(T).Name}Create";
        var subscriptionType = new ObjectTypeExtension(descriptor => descriptor
            .Name(OperationTypeNames.Subscription)
            .Field(fieldName)
            .Resolve(context => context.GetEventMessage<T>())
            .Subscribe(context => context.Services.GetRequiredService<IMongoCollection<T>>()
                .ObserveChanges(doc => doc.OperationType == ChangeStreamOperationType.Create)));

        builder.ConfigureSchema(b =>
            b.TryAddRootType(
            () => new global::HotChocolate.Types.ObjectType(
                d => d.Name(OperationTypeNames.Subscription)),
            Language.OperationType.Subscription));

        builder.AddTypeExtension(subscriptionType);
        return builder;
    }
}
