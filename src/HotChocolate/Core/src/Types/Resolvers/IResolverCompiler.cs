using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// This services provides access to the internal resolver compiler.
    /// </summary>
    public interface IResolverCompiler : IDisposable
    {
        /// <summary>
        /// Compiles a resolver from a member selector.
        /// </summary>
        /// <param name="propertyOrMethod">
        /// The member selector.
        /// </param>
        /// <param name="sourceType">
        /// The source type.
        /// </param>
        /// <typeparam name="TResolver">
        /// The resolver type.
        /// </typeparam>
        /// <returns>
        /// Returns a struct containing the compiled resolvers.
        /// </returns>
        FieldResolverDelegates CompileResolve<TResolver>(
            Expression<Func<TResolver, object?>> propertyOrMethod,
            Type? sourceType = null);

        /// <summary>
        /// Compiles a resolver from a member selector.
        /// </summary>
        /// <param name="lambda">
        /// The member selector.
        /// </param>
        /// <param name="sourceType">
        /// The source type.
        /// </param>
        /// <param name="resolverType">
        /// The resolver type.
        /// </param>
        /// <returns>
        /// Returns a struct containing the compiled resolvers.
        /// </returns>
        FieldResolverDelegates CompileResolve(
            LambdaExpression lambda,
            Type sourceType,
            Type? resolverType = null);

        /// <summary>
        /// Compiles a resolver from a member.
        /// </summary>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <param name="sourceType">
        /// The source type.
        /// </param>
        /// <param name="resolverType">
        /// The resolver type.
        /// </param>
        /// <returns>
        /// Returns a struct containing the compiled resolvers.
        /// </returns>
        FieldResolverDelegates CompileResolve(
            MemberInfo member,
            Type? sourceType = null,
            Type? resolverType = null);

        /// <summary>
        /// Compiles a subscribe resolver from a member.
        /// </summary>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <param name="sourceType">
        /// The source type.
        /// </param>
        /// <param name="resolverType">
        /// The resolver type.
        /// </param>
        /// <returns>
        /// Returns the compiled subscribe resolver.
        /// </returns>
        SubscribeResolverDelegate CompileSubscribe(
            MemberInfo member,
            Type? sourceType = null,
            Type? resolverType = null);

        /// <summary>
        /// Filters the specified arguments and returns only the parameters
        /// representing GraphQL field arguments.
        /// </summary>
        /// <param name="parameters">
        /// The method parameters.
        /// </param>
        /// <param name="sourceType">
        /// The source type.
        /// </param>
        /// <returns>
        /// Returns only the parameters representing GraphQL field arguments.
        /// </returns>
        IEnumerable<ParameterInfo> GetArgumentParameters(ParameterInfo[] parameters);
    }
}
