namespace Mocha;

[Flags]
internal enum DispatchMiddlewareStage
{
    Dispatch = 1,
    Preparation = 2,
    Delivery = 4
}
