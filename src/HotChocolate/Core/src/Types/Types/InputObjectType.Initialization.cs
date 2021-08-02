using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.Serialization.InputObjectCompiler;

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

        /// <summary>
        /// Initializes a new  instance of <see cref="InputObjectType"/>.
        /// </summary>
        protected InputObjectType()
        {
            _configure = Configure;
        }

        /// <summary>
        /// Initializes a new  instance of <see cref="InputObjectType"/>.
        /// </summary>
        /// <param name="configure">
        /// A delegate to specify the properties of this type.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <c>null</c>.
        /// </exception>
        public InputObjectType(Action<IInputObjectTypeDescriptor> configure)
        {
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        /// <summary>
        /// Create an input object type from a type definition.
        /// </summary>
        /// <param name="definition">
        /// The input object type definition that specifies the properties of the
        /// newly created input object type.
        /// </param>
        /// <returns>
        /// Returns the newly created input object type.
        /// </returns>
        public static InputObjectType CreateUnsafe(InputObjectTypeDefinition definition)
            => new() { Definition = definition};

        /// <summary>
        /// Override this in order to specify the type configuration explicitly.
        /// </summary>
        /// <param name="descriptor">
        /// The descriptor of this type lets you express the type configuration.
        /// </param>
        protected virtual void Configure(IInputObjectTypeDescriptor descriptor)
        {
        }

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

            ITypeConverter converter = context.Services.GetTypeConverter();

            SyntaxNode = definition.SyntaxNode;

            var fields = new InputField[definition.Fields.Count];
            OnCompleteFields(context, definition, ref fields);
            Fields = new FieldCollection<InputField>(fields);
            FieldInitHelper.CompleteFields(context, definition, Fields);

            if (RuntimeType == typeof(object) || Fields.Any(t => t.Property is null))
            {
                _createInstance = fieldValues =>
                {
                    var dictionary = new Dictionary<string, object?>();

                    foreach (var field in fields)
                    {
                        dictionary.Add(field.Name, fieldValues[field.Index]);
                    }

                    return dictionary;
                };
            }
            else
            {
                _createInstance = CompileFactory(this);
            }
        }

        protected virtual void OnCompleteFields(
            ITypeCompletionContext context,
            InputObjectTypeDefinition definition,
            ref InputField[] fields)
        {
            IEnumerable<InputFieldDefinition> fieldDefs = definition.Fields.Where(t => !t.Ignore);

            if (context.DescriptorContext.Options.SortFieldsByName)
            {
                fieldDefs = fieldDefs.OrderBy(t => t.Name);
            }

            var index = 0;
            foreach (var fieldDefinition in fieldDefs)
            {
                fields[index] = new(fieldDefinition, new(Name, fieldDefinition.Name), index);
                index++;
            }

            if (fields.Length > index)
            {
                Array.Resize(ref fields, index);
            }
        }
    }
}
