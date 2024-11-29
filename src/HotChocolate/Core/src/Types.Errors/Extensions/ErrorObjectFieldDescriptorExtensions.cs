namespace HotChocolate.Types;

/// <summary>
/// Provides extensions to the <see cref="IObjectFieldDescriptor"/> for the mutation convention.
/// </summary>
public static class ErrorObjectFieldDescriptorExtensions
{
    /// <summary>
    /// The <c>.Error&lt;TError&gt;()</c> extension method registers a middleware that will catch
    /// all exceptions of type <typeparamref name="TError"/> on mutations and queries.
    ///
    /// By applying the error extension to a fields the response type of the annotated resolver,
    /// will be automatically extended.
    /// </summary>
    public static IObjectFieldDescriptor Error<TError>(this IObjectFieldDescriptor descriptor) =>
        Error(descriptor, typeof(TError));

    /// <summary>
    /// The <c>.Error&lt;TError>()</c> extension method registers a middleware that will catch
    /// all exceptions of <paramref name="errorType"/> on mutations and queries.
    ///
    /// By applying the error extension to a fields the response type of the annotated resolver,
    /// will be automatically extended.
    /// </summary>
    public static IObjectFieldDescriptor Error(
        this IObjectFieldDescriptor descriptor,
        Type errorType)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (errorType is null)
        {
            throw new ArgumentNullException(nameof(errorType));
        }

        descriptor.Extend().OnBeforeCreate((ctx, d) => d.AddErrorType(ctx, errorType));

        return descriptor;
    }
}
