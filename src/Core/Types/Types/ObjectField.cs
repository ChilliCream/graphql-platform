using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public class ObjectField
        : OutputFieldBase<ObjectFieldDefinition>
        , IObjectField
    {
        private readonly static FieldDelegate _empty = c =>
            throw new InvalidOperationException();

        private readonly List<IDirective> _executableDirectives =
            new List<IDirective>();

        internal ObjectField(ObjectFieldDefinition definition)
            : base(definition)
        {
            Member = definition.Member;
            Expression = definition.Expression;
            Middleware = _empty;
            Resolver = definition.Resolver;
            SubscribeResolver = definition.SubscribeResolver;
            ExecutableDirectives = _executableDirectives.AsReadOnly();
        }

        public new ObjectType DeclaringType => (ObjectType)base.DeclaringType;

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
        public IReadOnlyCollection<IDirective> ExecutableDirectives { get; }

        /// <summary>
        /// Gets the associated .net type member of this field.
        /// This member can be <c>null</c>.
        /// </summary>
        public MemberInfo? Member { get; }

        [Obsolete("Use Member.")]
        public MemberInfo? ClrMember => Member;

        public Expression? Expression { get; }

        protected override void OnCompleteField(
            ICompletionContext context,
            ObjectFieldDefinition definition)
        {
            base.OnCompleteField(context, definition);

            CompleteExecutableDirectives(context);
            CompleteResolver(context, definition);
        }

        private void CompleteExecutableDirectives(
            ICompletionContext context)
        {
            var processed = new HashSet<string>();

            if (context.Type is ObjectType ot)
            {
                AddExectableDirectives(processed, ot.Directives);
                AddExectableDirectives(processed, Directives);
            }
        }

        private void AddExectableDirectives(
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
            ICompletionContext context,
            ObjectFieldDefinition definition)
        {
            bool isIntrospectionField = IsIntrospectionField || DeclaringType.IsIntrospectionType();

            Resolver = definition.Resolver;

            if (!isIntrospectionField || Resolver == null)
            {
                // gets resolvers that were provided via type extensions,
                // explicit resolver results or are provided through the
                // resolver compiler.
                FieldResolver resolver = context.GetResolver(definition.Name);
                Resolver = GetMostSpecificResolver(context.Type.Name, Resolver, resolver)!;
            }

            IReadOnlySchemaOptions options = context.DescriptorContext.Options;

            bool skipMiddleware =
                options.FieldMiddleware == FieldMiddlewareApplication.AllFields
                    ? false
                    : isIntrospectionField;

            var middlewareComponents = definition.MiddlewareComponents.Count == 0
                ? Array.Empty<FieldMiddleware>()
                : definition.MiddlewareComponents.ToArray();

            Middleware = FieldMiddlewareCompiler.Compile(
                context.GlobalComponents,
                middlewareComponents,
                Resolver,
                skipMiddleware);

            if (Resolver == null && Middleware == null)
            {
                if (_executableDirectives.Any())
                {
                    Middleware = ctx => Task.CompletedTask;
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
            if (currentResolver is null || externalCompiledResolver.TypeName.Equals(typeName))
            {
                return externalCompiledResolver.Resolver;
            }

            // in all other cases we will pick the internal resolver delegate.
            return currentResolver;
        }
    }
}
