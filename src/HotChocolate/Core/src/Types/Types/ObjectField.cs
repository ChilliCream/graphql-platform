using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

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
        private static readonly FieldDelegate _empty = c =>
            throw new InvalidOperationException();

        private readonly List<IDirective> _executableDirectives =
            new List<IDirective>();

        internal ObjectField(ObjectFieldDefinition definition, bool sortArgumentsByName = false)
            : base(definition, sortArgumentsByName)
        {
            Member = definition.Member ?? definition.ResolverMember;
            Middleware = _empty;
            Resolver = definition.Resolver!;
            SubscribeResolver = definition.SubscribeResolver;
            ExecutableDirectives = _executableDirectives.AsReadOnly();
            IsIntrospectionField = definition.IsIntrospectionField;
        }

        /// <summary>
        /// Gets the type that declares this field.
        /// </summary>
        public new ObjectType DeclaringType => (ObjectType)base.DeclaringType;

        IObjectType IObjectField.DeclaringType => DeclaringType;

        /// <summary>
        /// Gets the field resolver middleware.
        /// </summary>
        public FieldDelegate Middleware { get; private set; }

        /// <summary>
        /// Gets the field resolver.
        /// </summary>
        public FieldResolverDelegate Resolver { get; private set; }

        /// <summary>
        /// Gets the subscription resolver.
        /// </summary>
        public SubscribeResolverDelegate? SubscribeResolver { get; }

        /// <summary>
        /// Gets all executable directives that are associated with this field.
        /// </summary>
        public IReadOnlyList<IDirective> ExecutableDirectives { get; }

        /// <summary>
        /// Gets the associated .net type member of this field.
        /// This member can be <c>null</c>.
        /// </summary>
        public MemberInfo? Member { get; }

        /// <summary>
        /// Gets the associated resolver expression.
        /// This expression can be <c>null</c>.
        /// </summary>
        public Expression? Expression { get; }

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
            foreach (IDirective directive in directives.Where(t => t.IsExecutable))
            {
                if (!processed.Add(directive.Name) && !directive.Type.IsRepeatable)
                {
                    IDirective remove = _executableDirectives
                        .First(t => t.Name.Equals(directive.Name));
                    _executableDirectives.Remove(remove);
                }
                _executableDirectives.Add(directive);
            }
        }

        private void CompleteResolver(
            ITypeCompletionContext context,
            ObjectFieldDefinition definition)
        {
            var isIntrospectionField = IsIntrospectionField
                || DeclaringType.IsIntrospectionType();

            Resolver = definition.Resolver!;

            if (!isIntrospectionField || Resolver is null)
            {
                // gets resolvers that were provided via type extensions,
                // explicit resolver results or are provided through the
                // resolver compiler.
                FieldResolver resolver = context.GetResolver(definition.Name);
                Resolver = GetMostSpecificResolver(context.Type.Name, Resolver, resolver)!;
            }

            IReadOnlySchemaOptions options = context.DescriptorContext.Options;

            var skipMiddleware =
                options.FieldMiddleware != FieldMiddlewareApplication.AllFields &&
                isIntrospectionField;

            Middleware = FieldMiddlewareCompiler.Compile(
                context.GlobalComponents,
                definition.MiddlewareComponents.ToArray(),
                Resolver,
                skipMiddleware);

            if (Resolver is null && Middleware is null)
            {
                if (_executableDirectives.Count > 0)
                {
                    Middleware = ctx => default;
                }
                else
                {
                    context.ReportError(SchemaErrorBuilder.New()
                        .SetMessage(
                            $"The field `{context.Type.Name}.{Name}` " +
                            "has no resolver.")
                        .SetCode(ErrorCodes.Schema.NoResolver)
                        .SetTypeSystemObject(context.Type)
                        .AddSyntaxNode(definition.SyntaxNode)
                        .Build());
                }
            }
        }

        /// <summary>
        /// Gets the most relevant overwrite of a resolver.
        /// </summary>
        private static FieldResolverDelegate? GetMostSpecificResolver(
            NameString typeName,
            FieldResolverDelegate? currentResolver,
            FieldResolver? externalCompiledResolver)
        {
            // if there is no external compiled resolver then we will pick
            // the internal resolver delegate.
            if (externalCompiledResolver is null)
            {
                return currentResolver;
            }

            // if the internal resolver is null or if the external compiled
            // resolver represents an explicit overwrite of the type resolver
            // then we will pick the external compiled resolver.
            if (currentResolver is null
                || externalCompiledResolver.TypeName.Equals(typeName))
            {
                return externalCompiledResolver.Resolver;
            }

            // in all other cases we will pick the internal resolver delegate.
            return currentResolver;
        }

        public override string ToString() => $"{Name}:{Type.Visualize()}";
    }
}
