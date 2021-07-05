using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.Expressions;

#nullable enable

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// This class provides some helper methods to compile resolvers for dynamic schemas.
    /// </summary>
    internal sealed class DefaultResolverCompilerService : IResolverCompilerService
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
        public FieldResolverDelegates CompileResolve<TResolver>(
            Expression<Func<TResolver, object?>> propertyOrMethod,
            Type? sourceType = null)
            => ResolverCompiler.Resolve.Compile(propertyOrMethod, sourceType);

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
        public FieldResolverDelegates CompileResolve(
            MemberInfo member,
            Type? sourceType = null,
            Type? resolverType = null)
            => ResolverCompiler.Resolve.Compile(
                sourceType ?? member.ReflectedType ?? member.DeclaringType!,
                member,
                resolverType);

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
        public SubscribeResolverDelegate CompileSubscribe(
            MemberInfo member,
            Type? sourceType = null,
            Type? resolverType = null)
            => ResolverCompiler.Subscribe.Compile(
                sourceType ?? member.ReflectedType ?? member.DeclaringType!,
                resolverType,
                member);
    }
}
