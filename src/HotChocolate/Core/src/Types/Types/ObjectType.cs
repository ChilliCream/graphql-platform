using System.Globalization;
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

#nullable enable

namespace HotChocolate.Types
{
    public class ObjectType
        : NamedTypeBase<ObjectTypeDefinition>
        , IObjectType
        , IHasRuntimeType
        , IHasSyntaxNode
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
            IInitializationContext context)
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
            IInitializationContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
            SetTypeIdentity(typeof(ObjectType<>));
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            if (ValidateFields(context, definition))
            {
                _isOfType = definition.IsOfType;
                SyntaxNode = definition.SyntaxNode;

                var fields = new List<ObjectField>();
                AddIntrospectionFields(context, fields);
                AddRelayNodeField(context, fields);
                fields.AddRange(definition.Fields.Select(t => new ObjectField(t)));

                Fields = new FieldCollection<ObjectField>(fields);

                CompleteInterfacesHelper.Complete(
                    context, definition, RuntimeType, _interfaces, this, SyntaxNode);

                CompleteIsOfType(context);
                FieldInitHelper.CompleteFields(context, definition, Fields);
            }
        }

        private void AddIntrospectionFields(
            ICompletionContext context,
            ICollection<ObjectField> fields)
        {
            if (context.IsQueryType.HasValue && context.IsQueryType.Value)
            {
                fields.Add(new __SchemaField(context.DescriptorContext));
                fields.Add(new __TypeField(context.DescriptorContext));
            }

            fields.Add(new __TypeNameField(context.DescriptorContext));
        }

        private void AddRelayNodeField(
            ICompletionContext context,
            ICollection<ObjectField> fields)
        {
            if (context.IsQueryType.HasValue
                && context.IsQueryType.Value
                && context.ContextData.ContainsKey(RelayConstants.IsRelaySupportEnabled))
            {
                fields.Add(new NodeField(context.DescriptorContext));
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
            ICompletionContext context,
            ObjectTypeDefinition definition)
        {
            ObjectFieldDefinition[] invalidFields =
                definition.Fields.Where(t => t.Type is null).ToArray();

            foreach (ObjectFieldDefinition field in invalidFields)
            {
                // TODO : resources
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        "Unable to infer or resolve the type of " +
                        "field {0}.{1}. Try to explicitly provide the " +
                        "type like the following: " +
                        "`descriptor.Field(\"field\")" +
                        ".Type<List<StringType>>()`.",
                        Name,
                        field.Name))
                    .SetCode(ErrorCodes.Schema.NoFieldType)
                    .SetTypeSystemObject(this)
                    .SetPath(Path.New(Name).Append(field.Name))
                    .SetExtension(TypeErrorFields.Definition, field)
                    .Build());
            }

            return invalidFields.Length == 0;
        }

        private bool IsOfTypeWithClrType(
            IResolverContext context,
            object result)
        {
            if (result == null)
            {
                return true;
            }
            return RuntimeType.IsInstanceOfType(result);
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
    }
}
