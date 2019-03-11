using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class ObjectField
        : OutputFieldBase<ObjectFieldDefinition>
        , IObjectField
    {
        private readonly List<InterfaceField> _interfaceFields =
            new List<InterfaceField>();
        private readonly List<IDirective> _executableDirectives =
            new List<IDirective>();
        private readonly Type _resolverType;
        private readonly MemberInfo _member;

        internal ObjectField(ObjectFieldDefinition definition)
            : base(definition)
        {
            _resolverType = definition.ResolverType;
            _member = definition.Member;

            Resolver = definition.Resolver;
            InterfaceFields = _interfaceFields.AsReadOnly();
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
        /// Gets the interface fields that are implemented by this object field.
        /// </summary>
        public IReadOnlyCollection<InterfaceField> InterfaceFields { get; }

        /// <summary>
        /// Gets all executable directives that are associated with this field.
        /// </summary>
        public IReadOnlyCollection<IDirective> ExecutableDirectives { get; }

        /// <summary>
        /// Gets the associated .net type member of this field.
        /// This member can be <c>null</c>.
        /// </summary>
        public MemberInfo Member => _member;

        [Obsolete("Use Member.")]
        public MemberInfo ClrMember => Member;

        protected override void OnCompleteField(
            ICompletionContext context,
            ObjectFieldDefinition definition)
        {
            base.OnCompleteField(context, definition);

            CompleteInterfaceFields(context);
            CompleteExecutableDirectives(context);
            CompleteResolver(context, definition);
        }

        private void CompleteInterfaceFields(
            ICompletionContext context)
        {
            if (context.Type is ObjectType ot && ot.Interfaces.Count > 0)
            {
                foreach (InterfaceType interfaceType in ot.Interfaces.Values)
                {
                    if (interfaceType.Fields.TryGetField(Name,
                        out InterfaceField field))
                    {
                        _interfaceFields.Add(field);
                    }
                }
            }
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
            var fieldReference = new FieldReference(
                context.Type.Name, definition.Name);

            FieldResolver fieldResolver = context.GetResolver(fieldReference);
            Resolver = fieldResolver.Resolver;

            if (!IsIntrospectionField && !DeclaringType.IsIntrospectionType())
            {
                Middleware = context.GetCompiledMiddleware(fieldReference);
            }

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
