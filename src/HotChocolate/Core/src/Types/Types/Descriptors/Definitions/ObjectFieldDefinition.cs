using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// The <see cref="ObjectFieldDefinition"/> represents the configuration data for an
    /// output field (interface- or object-field).
    /// </summary>
    public class ObjectFieldDefinition : OutputFieldDefinitionBase
    {
        private List<FieldMiddleware>? _middlewareComponents;

        /// <summary>
        /// The object runtime type.
        /// </summary>
        public Type? SourceType { get; set; }

        /// <summary>
        /// The resolver type that exposes the resolver member.
        /// </summary>
        public Type? ResolverType { get; set; }

        /// <summary>
        /// The member on the <see cref="SourceType" />.
        /// </summary>
        public MemberInfo? Member { get; set; }

        /// <summary>
        /// The member that represents the resolver.
        /// </summary>
        public MemberInfo? ResolverMember { get; set; }

        /// <summary>
        /// The expression that represents the resolver.
        /// </summary>
        public Expression? Expression { get; set; }

        /// <summary>
        /// The result type of the resolver.
        /// </summary>
        public Type? ResultType { get; set; }

        /// <summary>
        /// The delegate that represents the resolver.
        /// </summary>
        public FieldResolverDelegate? Resolver { get; set; }

        /// <summary>
        /// The delegate that represents the pub-/sub-system subscribe delegate to open an
        /// event stream in case this field represents a subscription.
        /// </summary>
        public SubscribeResolverDelegate? SubscribeResolver { get; set; }

        /// <summary>
        /// A list of middleware components which will be used to form the field pipeline.
        /// </summary>
        public IList<FieldMiddleware> MiddlewareComponents =>
            _middlewareComponents ??= new List<FieldMiddleware>();

        /// <summary>
        /// Defines if this field configuration represents an introspection field.
        /// </summary>
        public bool IsIntrospectionField { get; internal set; }

        /// <summary>
        /// A list of middleware components which will be used to form the field pipeline.
        /// </summary>
        internal IReadOnlyList<FieldMiddleware> GetMiddlewareComponents()
        {
            if (_middlewareComponents is null)
            {
                return Array.Empty<FieldMiddleware>();
            }

            return _middlewareComponents;
        }
    }
}
