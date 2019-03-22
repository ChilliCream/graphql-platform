using System;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Bindings
{
    public class ComplexTypeBindingBuilder
        : IComplexTypeBindingBuilder
    {
        private readonly ComplexTypeBindingInfo _bindingInfo =
            new ComplexTypeBindingInfo();

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
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new ComplexTypeFieldBindingBuilder();
            configure(builder);

            if (builder.IsComplete())
            {
                _bindingInfo.Fields = _bindingInfo.Fields.Add(builder.Create());
                return this;
            }

            // TODO : resources
            throw new ArgumentException("notcompleted", nameof(builder));
        }

        public IComplexTypeBindingBuilder AddField(
            IComplexTypeFieldBindingBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (!builder.IsComplete())
            {
                // TODO : resources
                throw new ArgumentException("notcompleted", nameof(builder));
            }

            if (builder is ComplexTypeFieldBindingBuilder b)
            {
                _bindingInfo.Fields = _bindingInfo.Fields.Add(b.Create());
                return this;
            }

            // TODO : resources
            throw new NotSupportedException("builder not supported");
        }

        public bool IsComplete()
        {
            return _bindingInfo.IsValid();
        }

        public IBindingInfo Create()
        {
            return _bindingInfo.Clone();
        }

        public static ComplexTypeBindingBuilder New() =>
            new ComplexTypeBindingBuilder();
    }
}
