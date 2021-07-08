using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// The <see cref="ObjectFieldDefinition"/> contains the settings
    /// to create a <see cref="ObjectField"/>.
    /// </summary>
    public class ObjectFieldDefinition : OutputFieldDefinitionBase
    {
        private List<FieldMiddleware>? _middlewareComponents;
        private List<object>? _customSettings;

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
        /// </summary>
        public ObjectFieldDefinition() { }

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
        /// </summary>
        public ObjectFieldDefinition(
            NameString name,
            string? description = null,
            ITypeReference? type = null,
            FieldResolverDelegate? resolver = null,
            PureFieldDelegate? pureResolver = null)
        {
            Name = name;
            Description = description;
            Type = type;
            Resolver = resolver;
            PureResolver = pureResolver;
        }

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
        /// The delegate that represents an optional pure resolver.
        /// </summary>
        public PureFieldDelegate? PureResolver { get; set; }

        /// <summary>
        /// Gets or sets all resolvers at once.
        /// </summary>
        public FieldResolverDelegates Resolvers
        {
            get => GetResolvers();
            set
            {
                Resolver = value.Resolver;
                PureResolver = value.PureResolver;
            }
        }

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
        /// A list of custom settings objects that can be user in the type interceptors.
        /// Custom settings are not copied to the actual type system object.
        /// </summary>
        public IList<object> CustomSettings =>
            _customSettings ??= new List<object>();

        /// <summary>
        /// Defines if this field configuration represents an introspection field.
        /// </summary>
        public bool IsIntrospectionField { get; internal set; }

        /// <summary>
        /// Defines if this field can be executed in parallel with other fields.
        /// </summary>
        public bool IsParallelExecutable { get; set; } = true;

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

        /// <summary>
        /// A list of custom settings objects that can be user in the type interceptors.
        /// Custom settings are not copied to the actual type system object.
        /// </summary>
        internal IReadOnlyList<object> GetCustomSettings()
        {
            if (_customSettings is null)
            {
                return Array.Empty<object>();
            }

            return _customSettings;
        }

        internal FieldResolverDelegates GetResolvers() =>
            new(Resolver, PureResolver);

        internal void CopyTo(ObjectFieldDefinition target)
        {
            base.CopyTo(target);

            if (_middlewareComponents is { Count: > 0 })
            {
                target._middlewareComponents = new List<FieldMiddleware>(_middlewareComponents);
            }

            if (_customSettings is { Count: > 0 })
            {
                target._customSettings = new List<object>(_customSettings);
            }

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
            target.IsParallelExecutable = IsParallelExecutable;
        }

        internal void MergeInto(ObjectFieldDefinition target)
        {
            base.MergeInto(target);

            if (_middlewareComponents is { Count: > 0 })
            {
                target._middlewareComponents ??= new List<FieldMiddleware>();
                target._middlewareComponents.AddRange(_middlewareComponents);
            }

            if (_customSettings is { Count: > 0 })
            {
                target._customSettings ??= new List<object>();
                target._customSettings.AddRange(_customSettings);
            }

            if (!IsParallelExecutable)
            {
                target.IsParallelExecutable = false;
            }

            if (ResolverType is not null)
            {
                target.ResolverType = ResolverType;
            }

            if (Member is not null)
            {
                target.Member = Member;
            }

            if (ResolverMember is not null)
            {
                target.ResolverMember = ResolverMember;
            }

            if (Expression is not null)
            {
                target.Expression = Expression;
            }

            if (ResultType is not null)
            {
                target.ResultType = ResultType;
            }

            if (Resolver is not null)
            {
                target.Resolver = Resolver;
            }

            if (SubscribeResolver is not null)
            {
                target.SubscribeResolver = SubscribeResolver;
            }
        }
    }
}
