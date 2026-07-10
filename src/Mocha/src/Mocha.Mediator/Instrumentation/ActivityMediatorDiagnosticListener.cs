using System.Diagnostics;
using static Mocha.Mediator.MochaMediatorActivitySource;
using static Mocha.Mediator.SemanticConventions;

namespace Mocha.Mediator;

internal sealed class ActivityMediatorDiagnosticListener : MediatorDiagnosticEventListener
{
    public override IDisposable Execute(Type messageType, Type responseType, object message)
    {
        var messageTypeName = messageType.Name;
        var operationType = typeof(INotification).IsAssignableFrom(messageType)
            ? OperationTypePublish
            : OperationTypeSend;

        var activity = Source.StartActivity($"{messageTypeName} {operationType}");

        if (activity is null)
        {
            return EmptyScope;
        }

        activity.SetTag(MessagingSystem, MessagingSystemValue);
        activity.SetTag(MessagingOperationType, operationType);
        activity.SetTag(MessagingMessageType, messageTypeName);
        activity.SetStatus(ActivityStatusCode.Ok);

        return activity;
    }

    public override void ExecutionError(Type messageType, Type responseType, object message, Exception exception)
    {
        if (Activity.Current is not { } activity)
        {
            return;
        }

        var tags = new ActivityTagsCollection
        {
            { ExceptionType, exception.GetType().FullName },
            { ExceptionMessage, exception.Message }
        };

        activity.AddEvent(new ActivityEvent(ExceptionEventName, default, tags));
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }
}
