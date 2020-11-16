using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Properties;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Bindings
{
    public class ComplexTypeBindingBuilder
        : IComplexTypeBindingBuilder
    {
        private readonly ComplexTypeBindingInfo _bindingInfo =
            new ComplexTypeBindingInfo();
        private readonly List<ComplexTypeFieldBindingBuilder> _fields =
            new List<ComplexTypeFieldBindingBuilder>();

        public IComplexTypeBindingBuilder SetName(NameString typeName)
        {
            _bindingInfo.Name = typeName.EnsureNotEmpty(nameof(typeName));
            return this;
        }

        public IComplexTypeBindingBuilder SetType(Type type)
        {
            _bindingInfo.Type = type
                ?? throw new ArgumentNullException(nameof(type));
            return this;
        }

        public IComplexTypeBindingBuilder SetFieldBinding(
            BindingBehavior behavior)
        {
            _bindingInfo.BindingBehavior = behavior;
            return this;
        }

        public IComplexTypeBindingBuilder AddField(
            Action<IComplexTypeFieldBindingBuilder> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new ComplexTypeFieldBindingBuilder();
            configure(builder);

            if (builder.IsComplete())
            {
                _fields.Add(builder);
                return this;
            }

            throw new ArgumentException(
                TypeResources.ComplexTypeBindingBuilder_FieldNotComplete,
                nameof(configure));
        }

        public IComplexTypeBindingBuilder AddField(
            IComplexTypeFieldBindingBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (!builder.IsComplete())
            {
                throw new ArgumentException(
                    TypeResources.ComplexTypeBindingBuilder_FieldNotComplete,
                    nameof(builder));
            }

            if (builder is ComplexTypeFieldBindingBuilder b)
            {
                _fields.Add(b);
                return this;
            }

            throw new NotSupportedException(TypeResources
                .ComplexTypeBindingBuilder_FieldBuilderNotSupported);
        }

        public bool IsComplete()
        {
            if (_bindingInfo.BindingBehavior == BindingBehavior.Explicit
                && _fields.Count == 0)
            {
                return false;
            }

            if (_bindingInfo.Type is null)
            {
                return false;
            }

            return true;
        }

        public IBindingInfo Create()
        {
            ComplexTypeBindingInfo cloned = _bindingInfo.Clone();
            cloned.Fields = ImmutableList.CreateRange(
                _fields.Select(t => t.Create()));
            return cloned;
        }

        public static ComplexTypeBindingBuilder New() =>
            new ComplexTypeBindingBuilder();
    }
}
