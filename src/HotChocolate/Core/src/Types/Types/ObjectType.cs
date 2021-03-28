using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.FieldInitHelper;
using static HotChocolate.Types.CompleteInterfacesHelper;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Types
{
    public class ObjectType
        : NamedTypeBase<ObjectTypeDefinition>
        , IObjectType
    {
        private InterfaceType[] _implements = Array.Empty<InterfaceType>();
        private Action<IObjectTypeDescriptor>? _configure;
        private IsOfType? _isOfType;

        protected ObjectType()
        {
            _configure = Configure;
            Fields = FieldCollection<ObjectField>.Empty;
        }

        public ObjectType(Action<IObjectTypeDescriptor> configure)
        {
            _configure = configure;
            Fields = FieldCollection<ObjectField>.Empty;
        }

        public override TypeKind Kind => TypeKind.Object;

        public ObjectTypeDefinitionNode? SyntaxNode { get; private set; }

        ISyntaxNode? IHasSyntaxNode.SyntaxNode => SyntaxNode;

        public IReadOnlyList<InterfaceType> Implements => _implements;

        IReadOnlyList<IInterfaceType> IComplexOutputType.Implements => Implements;

        public FieldCollection<ObjectField> Fields { get; private set; }

        IFieldCollection<IObjectField> IObjectType.Fields => Fields;

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        public bool IsOfType(IResolverContext context, object resolverResult) =>
            _isOfType!.Invoke(context, resolverResult);

        public bool IsImplementing(NameString interfaceTypeName) =>
            _implements.Any(t => t.Name.Equals(interfaceTypeName));

        public bool IsImplementing(InterfaceType interfaceType) =>
            Array.IndexOf(_implements, interfaceType) != -1;

        public bool IsImplementing(IInterfaceType interfaceType) =>
            interfaceType is InterfaceType i && _implements.Contains(i);

        protected override ObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = ObjectTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType());
            _configure!.Invoke(descriptor);
            _configure = null;
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
            SetTypeIdentity(typeof(ObjectType<>));
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            if (ValidateFields(context, definition))
            {
                _isOfType = definition.IsOfType;
                SyntaxNode = definition.SyntaxNode;

                // create fields with the specified sorting settings ...
                var sortByName = context.DescriptorContext.Options.SortFieldsByName;
                var fields = definition.Fields.Where(t => !t.Ignore).Select(
                    t => new ObjectField(
                        t,
                        new FieldCoordinate(Name, t.Name),
                        sortByName)).ToList();
                Fields = FieldCollection<ObjectField>.From(fields, sortByName);

                // resolve interface references
                IReadOnlyList<ITypeReference> interfaces = definition.GetInterfaces();
                if (interfaces.Count > 0)
                {
                    var implements = new List<InterfaceType>();

                    CompleteInterfaces(
                        context,
                        interfaces,
                        RuntimeType,
                        implements,
                        this, SyntaxNode);

                    _implements = implements.ToArray();
                }

                // complete the type resolver and fields
                CompleteTypeResolver(context);
                CompleteFields(context, definition, Fields);
            }
        }

        private void CompleteTypeResolver(ITypeCompletionContext context)
        {
            if (_isOfType is null)
            {
                if (context.IsOfType != null)
                {
                    IsOfTypeFallback isOfType = context.IsOfType;
                    _isOfType = (ctx, obj) => isOfType(this, ctx, obj);
                }
                else if (RuntimeType == typeof(object))
                {
                    _isOfType = IsOfTypeWithName;
                }
                else
                {
                    _isOfType = IsOfTypeWithClrType;
                }
            }
        }

        private bool ValidateFields(
            ITypeCompletionContext context,
            ObjectTypeDefinition definition)
        {
            var hasErrors = false;

            foreach (ObjectFieldDefinition field in definition.Fields.Where(t => t.Type is null))
            {
                hasErrors = true;
                context.ReportError(ObjectType_UnableToInferOrResolveType(Name, this, field));
            }

            return !hasErrors;
        }

        private bool IsOfTypeWithClrType(
            IResolverContext context,
            object? result)
        {
            if (result is null)
            {
                return true;
            }

            return RuntimeType.IsInstanceOfType(result);
        }

        private bool IsOfTypeWithName(
            IResolverContext context,
            object? result)
        {
            if (result is null)
            {
                return true;
            }

            Type type = result.GetType();
            return Name.Equals(type.Name);
        }
    }
}
