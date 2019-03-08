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
        : OutputFieldBase
        , IObjectField
    {
        private readonly List<InterfaceField> _interfaceFields =
            new List<InterfaceField>();
        private readonly List<IDirective> _executableDirectives =
            new List<IDirective>();
        private List<FieldMiddleware> _middlewareComponents;
        private readonly Type _sourceType;
        private readonly Type _resolverType;
        private readonly MemberInfo _member;




        internal ObjectField(ObjectFieldDescription fieldDescription)
            : base(fieldDescription)
        {
            _sourceType = fieldDescription.SourceType ?? typeof(object);
            _resolverType = fieldDescription.ResolverType;
            _member = fieldDescription.ClrMember;
            _middlewareComponents = fieldDescription.MiddlewareComponents;

            Resolver = fieldDescription.Resolver;
            InterfaceFields = _interfaceFields.AsReadOnly();
            ExecutableDirectives = _executableDirectives.AsReadOnly();
        }

        private static ObjectFieldDescription ExecuteConfigure(
            NameString fieldName,
            Action<IObjectFieldDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var descriptor = new ObjectFieldDescriptor(fieldName);
            configure(descriptor);
            return descriptor.CreateDescription();
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
        public MemberInfo ClrMember => _member;

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            if (_member != null)
            {
                context.RegisterResolver(
                    _sourceType, _resolverType, Name, _member);
            }
        }

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            CompleteInterfaceFields(context);
            CompleteExecutableDirectives(context);
            CompleteResolver(context);
        }

        private void CompleteInterfaceFields(
            ITypeInitializationContext context)
        {
            if (context.Type is ObjectType ot && ot.Interfaces.Count > 0)
            {
                foreach (InterfaceType interfaceType in ot.Interfaces.Values)
                {
                    if (interfaceType.Fields
                        .TryGetField(Name, out InterfaceField field))
                    {
                        _interfaceFields.Add(field);
                    }
                }
            }
        }

        private void CompleteExecutableDirectives(
            ITypeInitializationContext context)
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
            ITypeInitializationContext context)
        {
            if (Resolver == null)
            {
                Resolver = context.GetResolver(Name);
            }

            Middleware = context.CreateMiddleware(
                _middlewareComponents, Resolver,
                IsIntrospectionField
                || DeclaringType.IsIntrospectionType());

            if (Resolver == null && Middleware == null)
            {
                if (_executableDirectives.Any())
                {
                    Middleware = ctx => Task.CompletedTask;
                }
                else
                {
                    context.ReportError(new SchemaError(
                        $"The field `{context.Type.Name}.{Name}` " +
                        "has no resolver.", (INamedType)context.Type));
                }
            }

            _middlewareComponents = null;
        }
    }
}
