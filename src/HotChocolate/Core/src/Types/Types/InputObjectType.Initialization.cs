using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Utilities.Serialization.InputObjectCompiler;
using static HotChocolate.Internal.FieldInitHelper;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL input object type
    /// </summary>
    public partial class InputObjectType
    {
        private Action<IInputObjectTypeDescriptor>? _configure;
        private Func<object?[], object> _createInstance = default!;
        private Action<object, object?[]> _getFieldValues = default!;

        protected override InputObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
        {
            try
            {
                if (Definition is null)
                {
                    var descriptor = InputObjectTypeDescriptor.FromSchemaType(
                        context.DescriptorContext,
                        GetType());
                    _configure!(descriptor);
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
            InputObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
            SetTypeIdentity(typeof(InputObjectType<>));
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            SyntaxNode = definition.SyntaxNode;
            Fields = OnCompleteFields(context, definition);

            if (RuntimeType == typeof(object) || Fields.Any(t => t.Property is null))
            {
                _createInstance = CreateDictionaryInstance;
                _getFieldValues = CreateDictionaryGetValues;
            }
            else
            {
                _createInstance = CompileFactory(this);
                _getFieldValues = CompileGetFieldValues(this);
            }
        }

        protected virtual FieldCollection<InputField> OnCompleteFields(
            ITypeCompletionContext context,
            InputObjectTypeDefinition definition)
        {
            return CompleteFields(context, this, definition.Fields, CreateField);
            static InputField CreateField(InputFieldDefinition fieldDef, int index)
                => new(fieldDef, index);
        }

        private object CreateDictionaryInstance(object?[] fieldValues)
        {
            var dictionary = new Dictionary<string, object?>();

            foreach (var field in Fields)
            {
                dictionary.Add(field.Name, fieldValues[field.Index]);
            }

            return dictionary;
        }

        private void CreateDictionaryGetValues(object obj, object?[] fieldValues)
        {
            var map = (Dictionary<string, object?>)obj;

            foreach (var field in Fields)
            {
                if (map.TryGetValue(field.Name, out var val))
                {
                    fieldValues[field.Index] = val;
                }
            }
        }
    }
}
