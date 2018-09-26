using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class ObjectField
        : ObjectFieldBase
    {
        private readonly List<InterfaceField> _interfaceFields =
            new List<InterfaceField>();
        private readonly Type _sourceType;
        private readonly Type _resolverType;
        private readonly MemberInfo _member;

        internal ObjectField(string fieldName,
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

            Resolver = fieldDescription.Resolver;
            InterfaceFields = _interfaceFields.AsReadOnly();
        }

        private static ObjectFieldDescription ExecuteConfigure(
            string fieldName,
            Action<IObjectFieldDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var descriptor = new ObjectFieldDescriptor(null, fieldName);
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
        public IReadOnlyCollection<InterfaceField> InterfaceFields { get; private set; }

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

            if (Resolver == null)
            {
                Resolver = context.GetResolver(Name);
                if (Resolver == null)
                {
                    context.ReportError(new SchemaError(
                        $"The field `{context.Type.Name}.{Name}` " +
                        "has no resolver.", context.Type));
                }
            }

            CompleteInterfaceFields(context);
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
    }
}
