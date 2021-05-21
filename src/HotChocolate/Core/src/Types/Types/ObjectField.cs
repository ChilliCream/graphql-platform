using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a field of an <see cref="ObjectType"/>.
    /// </summary>
    public class ObjectField
        : OutputFieldBase<ObjectFieldDefinition>
        , IObjectField
    {
        private static readonly FieldDelegate _empty = _ => throw new InvalidOperationException();
        private IDirective[] _executableDirectives = Array.Empty<IDirective>();

        internal ObjectField(
            ObjectFieldDefinition definition,
            FieldCoordinate fieldCoordinate,
            bool sortArgumentsByName = false)
            : base(definition, fieldCoordinate, sortArgumentsByName)
        {
            Member = definition.Member;
            ResolverMember = definition.ResolverMember ?? definition.Member;
            Middleware = _empty;
            Resolver = definition.Resolver!;
            ResolverExpression = definition.Expression;
            SubscribeResolver = definition.SubscribeResolver;
            IsIntrospectionField = definition.IsIntrospectionField;
            IsParallelExecutable = definition.IsParallelExecutable;
        }

        /// <summary>
        /// Gets the type that declares this field.
        /// </summary>
        public new ObjectType DeclaringType => (ObjectType)base.DeclaringType;

        IObjectType IObjectField.DeclaringType => DeclaringType;

        /// <summary>
        /// Defines if this field can be executed in parallel with other fields.
        /// </summary>
        public bool IsParallelExecutable { get; private set; }

        /// <summary>
        /// Gets the field resolver middleware.
        /// </summary>
        public FieldDelegate Middleware { get; private set; }

        /// <summary>
        /// Gets the field resolver.
        /// </summary>
        public FieldResolverDelegate? Resolver { get; private set; }

        /// <summary>
        /// Gets the pure field resolver. The pure field resolver is only available if this field
        /// can be resolved without side-effects. The execution engine will prefer this resolver
        /// variant if it is available and there are no executable directives that add a middleware
        /// to this field.
        /// </summary>
        public PureFieldDelegate? PureResolver { get; private set; }

        /// <summary>
        /// Gets a field resolver that can be used to inline the resolver execution into the parent
        /// resolver. Resolvers can only be inlined if they abide to the rules of the pure-resolver.
        /// Further inline resolvers cannot have arguments and do not have access to context data.
        /// </summary>
        public InlineFieldDelegate? InlineResolver { get; private set; }

        /// <summary>
        /// Gets the subscription resolver.
        /// </summary>
        public SubscribeResolverDelegate? SubscribeResolver { get; }

        /// <summary>
        /// Gets all executable directives that are associated with this field.
        /// </summary>
        public IReadOnlyList<IDirective> ExecutableDirectives => _executableDirectives;

        /// <summary>
        /// Gets the associated member of the runtime type for this field.
        /// This property can be <c>null</c> if this field is not associated to
        /// a concrete member on the runtime type.
        /// </summary>
        public MemberInfo? Member { get; }

        /// <summary>
        /// Gets the resolver member of this filed.
        /// If this field has no explicit resolver member
        /// this property will return <see cref="Member"/>.
        /// </summary>
        public MemberInfo? ResolverMember { get; }

        /// <summary>
        /// Gets the associated resolver expression.
        /// This expression can be <c>null</c>.
        /// </summary>
        [Obsolete("Use resolver expression.")]
        public Expression? Expression => ResolverExpression;

        /// <summary>
        /// Gets the associated resolver expression.
        /// This expression can be <c>null</c>.
        /// </summary>
        public Expression? ResolverExpression { get; }

        /// <summary>
        /// Defines if this field as a introspection field.
        /// </summary>
        public override bool IsIntrospectionField { get; }

        protected override void OnCompleteField(
            ITypeCompletionContext context,
            ObjectFieldDefinition definition)
        {
            base.OnCompleteField(context, definition);

            CompleteExecutableDirectives(context);
            CompleteResolver(context, definition);
        }

        private void CompleteExecutableDirectives(
            ITypeCompletionContext context)
        {
            var processed = new HashSet<string>();

            if (context.Type is ObjectType ot)
            {
                AddExecutableDirectives(processed, ot.Directives);
                AddExecutableDirectives(processed, Directives);
            }
        }

        private void AddExecutableDirectives(
            ISet<string> processed,
            IEnumerable<IDirective> directives)
        {
            List<IDirective>? executableDirectives = null;
            foreach (IDirective directive in directives.Where(t => t.Type.HasMiddleware))
            {
                executableDirectives ??= new List<IDirective>(_executableDirectives);
                if (!processed.Add(directive.Name) && !directive.Type.IsRepeatable)
                {
                    IDirective remove = executableDirectives
                        .First(t => t.Name.Equals(directive.Name));
                    executableDirectives.Remove(remove);
                }
                executableDirectives.Add(directive);
            }

            if (executableDirectives is not null)
            {
                _executableDirectives = executableDirectives.ToArray();
            }
        }

        private void CompleteResolver(
            ITypeCompletionContext context,
            ObjectFieldDefinition definition)
        {
            var isIntrospectionField = IsIntrospectionField || DeclaringType.IsIntrospectionType();
            IReadOnlyList<FieldMiddleware> fieldComponents = definition.GetMiddlewareComponents();
            IReadOnlySchemaOptions options = context.DescriptorContext.Options;
            NameString typeName = context.Type.Name;

            var skipMiddleware =
                options.FieldMiddleware != FieldMiddlewareApplication.AllFields &&
                isIntrospectionField;

            Resolver = definition.Resolver!;

            if (definition.PureResolver is not null && IsPureContext())
            {
                PureFieldResolverDelegate pure = definition.PureResolver;
                PureResolver = c => c.Result = pure(c);
            }

            if (definition.InlineResolver is not null && IsPureContext())
            {
                InlineResolver = definition.InlineResolver;
            }

            if (!isIntrospectionField || Resolver is null!)
            {
                // gets resolvers that were provided via type extensions,
                // explicit resolver results or are provided through the
                // resolver compiler.
                FieldResolver? resolver = context.GetResolver(definition.Name);
                Resolver = GetMostSpecificResolver(typeName, Resolver, resolver, out var external)!;

                // if a pure resolver variant is available and there are no global or field
                // middleware components than we will make the pure resolver variant
                // available on this field.
                PureFieldResolverDelegate? pureResolver =
                    definition.PureResolver is null &&
                    external &&
                    IsPureContext()
                        ? resolver?.PureResolver
                        : null;

                if (pureResolver is not null)
                {
                    PureResolver = c => c.Result = pureResolver(c);

                    if (context.DescriptorContext.Options.AllowInlining &&
                        InlineResolver is null &&
                        (definition.ResolverMember is null ||
                         ReferenceEquals(definition.Member, definition.ResolverMember)) &&
                        definition.Member is PropertyInfo property)
                    {
                        InlineResolver = CompileInlineResolver(property);
                    }
                }
            }

            // if we have an inline resolver we can always create from that
            // a pure resolver that can be used by the execution engine.
            if (InlineResolver is not null && PureResolver is null)
            {
                PureResolver = ctx => ctx.Result = InlineResolver(ctx.Parent<object>());
            }

            // by definition fields with pure resolvers are parallel executable.
            if (!IsParallelExecutable && PureResolver is not null)
            {
                IsParallelExecutable = true;
            }

            Middleware = FieldMiddlewareCompiler.Compile(
                context.GlobalComponents,
                fieldComponents,
                Resolver,
                skipMiddleware);

            if (Resolver is null! && Middleware is null)
            {
                if (_executableDirectives.Length > 0)
                {
                    Middleware = _ => default;
                }
                else
                {
                    context.ReportError(
                        ObjectField_HasNoResolver(
                            context.Type.Name,
                            Name,
                            context.Type,
                            SyntaxNode));
                }
            }

            bool IsPureContext()
            {
                return (skipMiddleware ||
                    (context.GlobalComponents.Count == 0 &&
                    fieldComponents.Count == 0 &&
                    _executableDirectives.Length == 0));
            }
        }

        /// <summary>
        /// Gets the most relevant overwrite of a resolver.
        /// </summary>
        private static FieldResolverDelegate? GetMostSpecificResolver(
            NameString typeName,
            FieldResolverDelegate? currentResolver,
            FieldResolver? externalCompiledResolver,
            out bool externalResolver)
        {
            // if there is no external compiled resolver then we will pick
            // the internal resolver delegate.
            if (externalCompiledResolver is null)
            {
                externalResolver = false;
                return currentResolver;
            }

            // if the internal resolver is null or if the external compiled
            // resolver represents an explicit overwrite of the type resolver
            // then we will pick the external compiled resolver.
            if (currentResolver is null || externalCompiledResolver.TypeName.Equals(typeName))
            {
                externalResolver = true;
                return externalCompiledResolver.Resolver;
            }

            // in all other cases we will pick the internal resolver delegate.
            externalResolver = false;
            return currentResolver;
        }

        /// <summary>
        /// This helper method can compile a inline resolver from a property.
        /// </summary>
        private static InlineFieldDelegate CompileInlineResolver(PropertyInfo property)
        {
            const string parent = nameof(parent);
            ParameterExpression parentParam = Expression.Parameter(typeof(object), parent);
            Expression castParent = Expression.Convert(parentParam, property.DeclaringType!);
            MemberExpression propertyAccessor = Expression.Property(castParent, property);
            Expression result = Expression.Convert(propertyAccessor, typeof(object));
            return Expression.Lambda<InlineFieldDelegate>(result, parentParam).Compile();
        }

        public override string ToString() => $"{Name}:{Type.Visualize()}";
    }
}
