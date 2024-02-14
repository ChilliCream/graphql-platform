namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class RequestMiddlewareInfo(
    string name,
    string typeName,
    string invokeMethodName,
    List<RequestMiddlewareParameterInfo> ctorParameters,
    List<RequestMiddlewareParameterInfo> invokeParameters)
{
    public string Name { get; } = name;

    public string TypeName { get; } = typeName;

    public string InvokeMethodName { get; } = invokeMethodName;

    public List<RequestMiddlewareParameterInfo> CtorParameters { get; } = ctorParameters;

    public List<RequestMiddlewareParameterInfo> InvokeParameters { get; } = invokeParameters;
}