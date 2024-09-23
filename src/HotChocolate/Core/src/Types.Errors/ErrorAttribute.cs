namespace HotChocolate.Types;

/// <summary>
/// The <see cref="ErrorAttribute"/> registers a middleware that will catch all exceptions of
/// type <see cref="ErrorAttribute.ErrorType"/> on mutations and queries.
///
/// By annotating the attribute the response type of the annotated resolver, will be automatically extended.
/// </summary>
public class ErrorAttribute : ObjectFieldDescriptorAttribute
{
    /// <inheritdoc cref="ErrorAttribute"/>
    /// <param name="errorType">
    /// The type of the exception, the class with factory methods or the error with an exception
    /// as the argument. See the examples in <see cref="ErrorAttribute"/>.
    /// </param>
    public ErrorAttribute(Type errorType)
    {
        ErrorType = errorType;
    }

    /// <summary>
    /// The type of the exception, the class with factory methods or the error with an exception
    /// as the argument. See the examples in <see cref="ErrorAttribute"/>.
    /// </summary>
    public Type ErrorType { get; }

    /// <inheritdoc />
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
        => descriptor.Error(ErrorType);
}

/// <summary>
/// The <see cref="ErrorAttribute{T}"/> registers a middleware that will catch all exceptions of
/// type <see cref="ErrorAttribute.ErrorType"/> on mutations and queries.
///
/// By annotating the attribute the response type of the annotated resolver, will be automatically extended.
/// </summary>
public sealed class ErrorAttribute<TError> : ErrorAttribute
{
    /// <inheritdoc cref="ErrorAttribute"/>
    public ErrorAttribute() : base(typeof(TError))
    {
    }
}
