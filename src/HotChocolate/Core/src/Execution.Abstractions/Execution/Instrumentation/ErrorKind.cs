namespace HotChocolate.Execution.Instrumentation;

// naming: is that maybe the error area?
public enum ErrorKind
{
    SyntaxError,
    ValidationError,
    RequestError,
    FieldError,
    SubscriptionEventError,
    OtherError
}
