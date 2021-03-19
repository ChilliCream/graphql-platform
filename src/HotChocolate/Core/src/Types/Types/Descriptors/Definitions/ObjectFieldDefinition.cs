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
        /// Defines a binding to another object field.
        /// </summary>
        public ObjectFieldBinding? BindTo { get; set; }

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

        public void CopyTo(ObjectFieldDefinition target)
        {
            base.CopyTo(target);

            target._middlewareComponents = _middlewareComponents;
            target.SourceType = SourceType;
            target.ResolverType = ResolverType;
            target.Member = Member;
            target.BindTo = BindTo;
            target.ResolverMember = ResolverMember;
            target.Expression = Expression;
            target.ResultType = ResultType;
            target.Resolver = Resolver;
            target.SubscribeResolver = SubscribeResolver;
            target.IsIntrospectionField = IsIntrospectionField;
        }
    }

    /// <summary>
    /// Describes a binding to an object field.
    /// </summary>
    public readonly struct ObjectFieldBinding
    {
        /// <summary>
        /// Creates a new instance of <see cref="ObjectFieldBinding"/>.
        /// </summary>
        /// <param name="name">
        /// The binding name.
        /// </param>
        /// <param name="type">
        /// The binding type.
        /// </param>
        public ObjectFieldBinding(NameString name, ObjectFieldBindingType type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Gets the binding name.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the binding type.
        /// </summary>
        public ObjectFieldBindingType Type { get; }
    }

    /// <summary>
    /// Describes what a field filter binds to.
    /// </summary>
    public enum ObjectFieldBindingType
    {
        /// <summary>
        /// Binds to a property
        /// </summary>
        Property,

        /// <summary>
        /// Binds to a GraphQL field
        /// </summary>
        Field
    }
}
