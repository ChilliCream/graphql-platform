using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Types
{
    public class ObjectType
        : NamedTypeBase<ObjectTypeDefinition>
        , IObjectType
    {
        private readonly List<InterfaceType> _interfaces = new List<InterfaceType>();
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

        public IReadOnlyList<InterfaceType> Interfaces => _interfaces;

        IReadOnlyList<IInterfaceType> IComplexOutputType.Interfaces => Interfaces;

        public FieldCollection<ObjectField> Fields { get; private set; }

        IFieldCollection<IObjectField> IObjectType.Fields => Fields;

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        public bool IsOfType(IResolverContext context, object resolverResult) =>
            _isOfType!.Invoke(context, resolverResult);

        public bool IsImplementing(NameString interfaceTypeName) =>
            _interfaces.Any(t => t.Name.Equals(interfaceTypeName));

        public bool IsImplementing(InterfaceType interfaceType) =>
            _interfaces.IndexOf(interfaceType) != -1;

        public bool IsImplementing(IInterfaceType interfaceType) =>
            interfaceType is InterfaceType i && _interfaces.IndexOf(i) != -1;

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

                var sortByName = context.DescriptorContext.Options.SortFieldsByName;
                var fields = definition.Fields.Select(t => new ObjectField(t, sortByName)).ToList();
                Fields = new FieldCollection<ObjectField>(fields, sortByName);

                CompleteInterfacesHelper.Complete(
                    context, definition, RuntimeType, _interfaces, this, SyntaxNode);

                CompleteIsOfType(context);
                FieldInitHelper.CompleteFields(context, definition, Fields);
            }
        }

        private void CompleteIsOfType(ITypeCompletionContext context)
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
            ObjectFieldDefinition[] invalidFields =
                definition.Fields.Where(t => t.Type is null).ToArray();

            foreach (ObjectFieldDefinition field in invalidFields)
            {
                context.ReportError(ObjectType_UnableToInferOrResolveType(Name, this, field));
            }

            return invalidFields.Length == 0;
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
