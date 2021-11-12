using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types;

public static class ResolveWithObjectFieldDescriptorExtensions
{
    /// <summary>
    /// Resolve a <typeparamref name="TResult"/> using the <paramref name="propertyOrMethod"/> 
    /// member selector on <typeparamref name="TResolver"/>.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="propertyOrMethod">
    /// The member selector.
    /// </param>
    /// <typeparam name="TResolver">
    /// The resolver type.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The result type.
    /// </typeparam>
    /// <returns></returns>
    public static IObjectFieldDescriptor ResolveWith<TResolver, TResult>(
        this IObjectFieldDescriptor descriptor,
        Expression<Func<TResolver, TResult?>> propertyOrMethod)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        UnaryExpression? convertedBody = Expression.Convert(propertyOrMethod.Body, typeof(object));
        var newExpression = Expression.Lambda<Func<TResolver, object?>>(convertedBody,
            propertyOrMethod.Parameters);

        return descriptor.ResolveWith(newExpression);
    }

    /// <summary>
    /// Asynchronously resolve a <typeparamref name="TResult"/> using the 
    /// <paramref name="propertyOrMethod"/> member selector on <typeparamref name="TResolver"/>.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="propertyOrMethod">
    /// The member selector.
    /// </param>
    /// <typeparam name="TResolver">
    /// The resolver type.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The result type.
    /// </typeparam>
    /// <returns></returns>
    public static IObjectFieldDescriptor ResolveWith<TResolver, TResult>(
        this IObjectFieldDescriptor descriptor,
        Expression<Func<TResolver, Task<TResult?>>> propertyOrMethod)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        UnaryExpression? convertedBody = Expression.Convert(propertyOrMethod.Body, typeof(object));
        var newExpression = Expression.Lambda<Func<TResolver, object?>>(convertedBody,
            propertyOrMethod.Parameters);

        return descriptor.ResolveWith(newExpression);
    }
}
