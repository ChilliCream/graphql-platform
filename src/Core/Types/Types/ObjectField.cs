using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class ObjectField
        : OutputFieldBase<ObjectFieldDefinition>
        , IObjectField
    {
        private readonly List<IDirective> _executableDirectives =
            new List<IDirective>();

        internal ObjectField(ObjectFieldDefinition definition)
            : base(definition)
        {
            Member = definition.Member;
            Resolver = definition.Resolver;
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
        /// Gets all executable directives that are associated with this field.
        /// </summary>
        public IReadOnlyCollection<IDirective> ExecutableDirectives { get; }

        /// <summary>
        /// Gets the associated .net type member of this field.
        /// This member can be <c>null</c>.
        /// </summary>
        public MemberInfo Member { get; }

        [Obsolete("Use Member.")]
        public MemberInfo ClrMember => Member;

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
            foreach (IDirective directive in
                directives.Where(t => t.IsExecutable))
            {
                if (!processed.Add(directive.Name)
                    && !directive.Type.IsRepeatable)
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
            bool isIntrospectionField = IsIntrospectionField
                || DeclaringType.IsIntrospectionType();

            Resolver = definition.Resolver;

            if (Resolver == null || !isIntrospectionField)
            {
                var fieldReference = new FieldReference(
                    context.Type.Name, definition.Name);
                FieldResolver resolver = context.GetResolver(fieldReference);
                if (resolver != null)
                {
                    Resolver = resolver.Resolver;
                }
            }

            Middleware = FieldMiddlewareCompiler.Compile(
                context.GlobalComponents,
                definition.MiddlewareComponents.ToArray(),
                Resolver,
                isIntrospectionField);

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
                        .SetCode(TypeErrorCodes.NoResolver)
                        .SetTypeSystemObject(context.Type)
                        .AddSyntaxNode(definition.SyntaxNode)
                        .Build());
                }
            }
        }
    }
}
