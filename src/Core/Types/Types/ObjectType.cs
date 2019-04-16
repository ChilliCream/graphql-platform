using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;
using HotChocolate.Types.Relay;

namespace HotChocolate.Types
{
    public class ObjectType
        : NamedTypeBase<ObjectTypeDefinition>
        , IComplexOutputType
        , IHasClrType
        , IHasSyntaxNode
    {
        private readonly Dictionary<NameString, InterfaceType> _interfaces =
            new Dictionary<NameString, InterfaceType>();
        private readonly Action<IObjectTypeDescriptor> _configure;
        private IsOfType _isOfType;

        protected ObjectType()
        {
            _configure = Configure;
        }

        public ObjectType(Action<IObjectTypeDescriptor> configure)
        {
            _configure = configure;
        }

        public override TypeKind Kind => TypeKind.Object;

        public ObjectTypeDefinitionNode SyntaxNode { get; private set; }

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        public IReadOnlyDictionary<NameString, InterfaceType> Interfaces =>
            _interfaces;

        public FieldCollection<ObjectField> Fields { get; private set; }

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        public bool IsOfType(IResolverContext context, object resolverResult)
            => _isOfType(context, resolverResult);

        #region Initialization

        protected override ObjectTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = ObjectTypeDescriptor.New(
                DescriptorContext.Create(context.Services),
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            _isOfType = definition.IsOfType;
            SyntaxNode = definition.SyntaxNode;

            var fields = new List<ObjectField>();
            AddIntrospectionFields(context, fields);
            AddRelayNodeField(context, fields);
            fields.AddRange(definition.Fields.Select(t => new ObjectField(t)));

            Fields = new FieldCollection<ObjectField>(fields);

            CompleteInterfaces(context, definition);
            CompleteIsOfType(context);
            FieldInitHelper.CompleteFields(context, definition, Fields);
        }

        private void AddIntrospectionFields(
            ICompletionContext context,
            ICollection<ObjectField> fields)
        {
            IDescriptorContext descriptorContext =
                DescriptorContext.Create(context.Services);

            if (context.IsQueryType.HasValue && context.IsQueryType.Value)
            {
                fields.Add(new __SchemaField(descriptorContext));
                fields.Add(new __TypeField(descriptorContext));
            }

            fields.Add(new __TypeNameField(descriptorContext));
        }

        private void AddRelayNodeField(
            ICompletionContext context,
            ICollection<ObjectField> fields)
        {
            if (context.IsQueryType.HasValue
                && context.IsQueryType.Value
                && context.ContextData.ContainsKey(
                    RelayConstants.IsRelaySupportEnabled))
            {
                fields.Add(new NodeField(
                    DescriptorContext.Create(context.Services)));
            }
        }

        private void CompleteInterfaces(
            ICompletionContext context,
            ObjectTypeDefinition definition)
        {
            if (ClrType != typeof(object))
            {
                foreach (Type interfaceType in ClrType.GetInterfaces())
                {
                    if (context.TryGetType(
                        new ClrTypeReference(interfaceType, TypeContext.Output),
                        out InterfaceType type))
                    {
                        _interfaces[type.Name] = type;
                    }
                }
            }

            foreach (ITypeReference interfaceRef in definition.Interfaces)
            {
                if (!context.TryGetType(interfaceRef, out InterfaceType type))
                {
                    context.ReportError(SchemaErrorBuilder.New()
                        .SetMessage(
                           "COULD NOT RESOLVE INTERFACE")
                        .SetCode(TypeErrorCodes.MissingType)
                        .SetTypeSystemObject(this)
                        .AddSyntaxNode(SyntaxNode)
                        .Build());
                }

                _interfaces[type.Name] = type;
            }
        }

        private void CompleteIsOfType(ICompletionContext context)
        {
            if (_isOfType == null)
            {
                if (context.IsOfType != null)
                {
                    IsOfTypeFallback isOfType = context.IsOfType;
                    _isOfType = (ctx, obj) => isOfType(this, ctx, obj);
                }
                else if (ClrType == typeof(object))
                {
                    _isOfType = IsOfTypeWithName;
                }
                else
                {
                    _isOfType = IsOfTypeWithClrType;
                }
            }
        }

        private bool IsOfTypeWithClrType(
            IResolverContext context,
            object result)
        {
            if (result == null)
            {
                return true;
            }
            return ClrType.IsInstanceOfType(result);
        }

        private bool IsOfTypeWithName(
            IResolverContext context,
            object result)
        {
            if (result == null)
            {
                return true;
            }

            Type type = result.GetType();
            return Name.Equals(type.Name);
        }

        #endregion
    }
}
