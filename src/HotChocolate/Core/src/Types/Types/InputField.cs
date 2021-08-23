using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types
{
    public class InputField
        : FieldBase<IInputType, InputFieldDefinition>
        , IInputField
    {
        public InputField(InputFieldDefinition definition, FieldCoordinate fieldCoordinate)
            : base(definition, fieldCoordinate)
        {
            SyntaxNode = definition.SyntaxNode;
            DefaultValue = definition.DefaultValue;
            Property = definition.Property;

            IReadOnlyList<IInputValueFormatter> formatters = definition.GetFormatters();
            Formatter = formatters.Count switch
            {
                0 => null,
                1 => formatters[0],
                _ => new AggregateInputValueFormatter(formatters)
            };

            Type? propertyType = definition.Property?.PropertyType;

            if (propertyType is { IsGenericType: true } &&
                propertyType.GetGenericTypeDefinition() == typeof(Optional<>))
            {
                IsOptional = true;
            }
        }

        /// <summary>
        /// The associated syntax node from the GraphQL SDL.
        /// </summary>
        public InputValueDefinitionNode? SyntaxNode { get; }

        /// <inheritdoc />
        public IValueNode? DefaultValue { get; private set; }

        /// <inheritdoc />
        public IInputValueFormatter? Formatter { get; }

        protected internal PropertyInfo? Property { get; }

        protected internal bool IsOptional { get; }

        public new InputObjectType DeclaringType =>
            (InputObjectType)base.DeclaringType;

        public override Type RuntimeType
        {
            get
            {
                return Property is null
                    ? base.RuntimeType
                    : Property.PropertyType;
            }
        }

        public void SetValue(object obj, object? value)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var success = Property is null
                ? TrySetValueOnUnknownType(obj, value)
                : TrySetValueOnKnownType(obj, value);

            if (!success)
            {
                throw new InvalidOperationException(
                    TypeResources.InputField_CannotSetValue);
            }
        }

        private bool TrySetValueOnUnknownType(object obj, object? value)
        {
            if (obj is IDictionary<string, object?> dict)
            {
                dict[Name] = value;
                return true;
            }

            ILookup<string, PropertyInfo> properties =
                ReflectionUtils.CreatePropertyLookup(obj.GetType());

            if (properties[Name].FirstOrDefault() is { } p)
            {
                p.SetValue(obj, value);
                return true;
            }

            return false;
        }

        private bool TrySetValueOnKnownType(object obj, object? value)
        {
            Property!.SetValue(obj, value);
            return true;
        }

        public object? GetValue(object obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            bool success = Property is null
                ? TryGetValueOnUnknownType(obj, out object? value)
                : TryGetValueOnKnownType(obj, out value);

            return success ? value : null;
        }

        public bool TryGetValue(object obj, out object? value)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return Property is null
                ? TryGetValueOnUnknownType(obj, out value)
                : TryGetValueOnKnownType(obj, out value);
        }

        private bool TryGetValueOnUnknownType(object obj, out object? value)
        {
            if (obj is IDictionary<string, object> d)
            {
                return d.TryGetValue(Name, out value);
            }

            ILookup<string, PropertyInfo> properties =
                ReflectionUtils.CreatePropertyLookup(obj.GetType());

            if (properties[Name].FirstOrDefault() is { } p)
            {
                value = p.GetValue(obj);
                return true;
            }

            value = null;
            return false;
        }

        private bool TryGetValueOnKnownType(object obj, out object? value)
        {
            value = Property!.GetValue(obj);
            return true;
        }

        protected override void OnCompleteField(
            ITypeCompletionContext context,
            InputFieldDefinition definition)
        {
            base.OnCompleteField(context, definition);
            DefaultValue = FieldInitHelper.CreateDefaultValue(
                context, definition, Type, Coordinate);
        }
    }
}
