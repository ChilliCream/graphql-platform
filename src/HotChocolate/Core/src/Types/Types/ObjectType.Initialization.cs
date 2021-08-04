using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.Helpers.FieldInitHelper;
using static HotChocolate.Types.Helpers.CompleteInterfacesHelper;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Types
{
    public partial class ObjectType
    {
        private InterfaceType[] _implements = Array.Empty<InterfaceType>();
        private Action<IObjectTypeDescriptor>? _configure;
        private IsOfType? _isOfType;

        protected override ObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            try
            {
                if (Definition is null)
                {
                    var descriptor = ObjectTypeDescriptor.FromSchemaType(
                        context.DescriptorContext,
                        GetType());
                    _configure!.Invoke(descriptor);
                    return descriptor.CreateDefinition();
                }

                return Definition;
            }
            finally
            {
                _configure = null;
            }
        }

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
                Fields = CompleteFields(context, this, definition.Fields, CreateField);
                _implements = CompleteInterfaces(context, definition.GetInterfaces(), this);
                CompleteTypeResolver(context);
            }

            static ObjectField CreateField(ObjectFieldDefinition fieldDef, int index)
                => new(fieldDef, index);
        }

        protected virtual void OnCompleteFields(
            ITypeCompletionContext context,
            ObjectTypeDefinition definition,
            ref ObjectField[] fields)
        {
            IEnumerable<ObjectFieldDefinition> fieldDefs = definition.Fields.Where(t => !t.Ignore);

            if (context.DescriptorContext.Options.SortFieldsByName)
            {
                fieldDefs = fieldDefs.OrderBy(t => t.Name);
            }

            var index = 0;
            foreach (var fieldDefinition in fieldDefs)
            {
                fields[index] = new(fieldDefinition, index);
                index++;
            }

            if (fields.Length > index)
            {
                Array.Resize(ref fields, index);
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
                    _isOfType = IsOfTypeWithRuntimeType;
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

        private bool IsOfTypeWithRuntimeType(
            IResolverContext context,
            object? result) =>
            result is null || RuntimeType == result.GetType();

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
