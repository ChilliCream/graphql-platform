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

        var activity = Source.StartActivity($"{operationType} {messageTypeName}");

        if (activity is null)
        {
            return EmptyScope;
        }

        activity.SetTag(MessagingSystem, MessagingSystemValue);
        activity.SetTag(MessagingOperationName, operationType);
        activity.SetTag(MessagingOperationType, operationType);
        activity.SetTag(MessagingMessageType, messageTypeName);

        return new ExecuteScope(activity);
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

    private sealed class ExecuteScope(Activity activity) : IDisposable
    {
        public void Dispose()
        {
            if (activity.Status != ActivityStatusCode.Error)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }

            activity.Dispose();
        }
    }
}
