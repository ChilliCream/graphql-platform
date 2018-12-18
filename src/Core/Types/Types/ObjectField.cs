using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class ObjectField
        : ObjectFieldBase
    {
        private readonly List<InterfaceField> _interfaceFields =
            new List<InterfaceField>();
        private readonly List<IDirective> _executableDirectives =
            new List<IDirective>();
        private List<FieldMiddleware> _middlewareComponents;
        private readonly Type _sourceType;
        private readonly Type _resolverType;
        private readonly MemberInfo _member;

        internal ObjectField(NameString fieldName,
            Action<IObjectFieldDescriptor> configure)
            : this(() => ExecuteConfigure(fieldName, configure))
        {
        }

        internal ObjectField(Func<ObjectFieldDescription> descriptionFactory)
            : this(DescriptorHelpers.ExecuteFactory(descriptionFactory))
        {
        }

        internal ObjectField(ObjectFieldDescription fieldDescription)
            : base(fieldDescription)
        {
            _sourceType = fieldDescription.SourceType;
            _resolverType = fieldDescription.ResolverType;
            _member = fieldDescription.Member;
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

        /// <summary>
        /// Gets the field resolver.
        /// </summary>
        /// <value></value>
        public FieldResolverDelegate Resolver { get; private set; }

        /// <summary>
        /// Gets the interface fields that are implemented by this object field.
        /// </summary>
        public IReadOnlyCollection<InterfaceField> InterfaceFields { get; }

        /// <summary>
        /// Gets all executable directives that are associated with this field.
        /// </summary>
        /// <value></value>
        public IReadOnlyCollection<IDirective> ExecutableDirectives { get; }

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
            HashSet<string> processed = new HashSet<string>();
            if (context.Type is ObjectType ot)
            {
                AddExectableDirectives(processed, ot.Directives);
                AddExectableDirectives(processed, Directives);
            }
        }

        private void AddExectableDirectives(
            HashSet<string> processed,
            IEnumerable<IDirective> directives)
        {
            foreach (IDirective directive in
                directives.Where(t => t.IsExecutable))
            {
                if (processed.Contains(directive.Name))
                {
                    _executableDirectives.Remove(directive);
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

            // TODO : review if that is what we want. Sometimes a middleware can replace a resolver ... but not all middleware components are resolver replacements.
            Resolver = context.CreateFieldMiddleware(
                _middlewareComponents, Resolver);

            // TODO : All executable middlewars should have a middleware so could we rewrite this?
            if (Resolver == null && _executableDirectives.All(
                t => t.Middleware == null))
            {
                context.ReportError(new SchemaError(
                    $"The field `{context.Type.Name}.{Name}` " +
                    "has no resolver.", context.Type));
            }

            _middlewareComponents = null;
        }
    }
}
