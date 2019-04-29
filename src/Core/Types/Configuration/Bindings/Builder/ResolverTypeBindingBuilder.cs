using System;
using System.Linq;
using HotChocolate.Properties;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Bindings
{
    public class ResolverTypeBindingBuilder
        : IResolverTypeBindingBuilder
    {
        private readonly ResolverTypeBindingInfo _bindingInfo =
            new ResolverTypeBindingInfo();

        public IResolverTypeBindingBuilder SetType(NameString typeName)
        {
            _bindingInfo.TypeName = typeName.EnsureNotEmpty(nameof(typeName));
            return this;
        }

        public IResolverTypeBindingBuilder SetType(Type type)
        {
            _bindingInfo.SourceType = type
                ?? throw new ArgumentNullException(nameof(type));
            return this;
        }

        public IResolverTypeBindingBuilder SetResolverType(Type type)
        {
            _bindingInfo.ResolverType = type
                ?? throw new ArgumentNullException(nameof(type));
            return this;
        }

        public IResolverTypeBindingBuilder SetFieldBinding(
            BindingBehavior behavior)
        {
            _bindingInfo.BindingBehavior = behavior;
            return this;
        }

        public IResolverTypeBindingBuilder AddField(
            Action<IResolverFieldBindingBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new ResolverFieldBindingBuilder();
            configure(builder);

            if (builder.IsComplete())
            {
                _bindingInfo.Fields = _bindingInfo.Fields.Add(builder.Create());
                return this;
            }

            throw new ArgumentException(
                TypeResources.ResolverTypeBindingBuilder_FieldNotComplete,
                nameof(builder));
        }

        public IResolverTypeBindingBuilder AddField(
            IResolverFieldBindingBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (!builder.IsComplete())
            {
                throw new ArgumentException(
                    TypeResources.ResolverTypeBindingBuilder_FieldNotComplete,
                    nameof(builder));
            }

            if (builder is ResolverFieldBindingBuilder b)
            {
                _bindingInfo.Fields = _bindingInfo.Fields.Add(b.Create());
                return this;
            }

            throw new NotSupportedException(
                TypeResources.ResolverTypeBindingBuilder_FieldBuilderNotSupported);
        }

        public bool IsComplete()
        {
            if (_bindingInfo.BindingBehavior == BindingBehavior.Explicit
                && _bindingInfo.Fields.Count == 0)
            {
                return false;
            }

            if (_bindingInfo.ResolverType == null)
            {
                return false;
            }

            return _bindingInfo.Fields.All(t => t.IsValid());
        }

        public IBindingInfo Create()
        {
            ResolverTypeBindingInfo cloned = _bindingInfo.Clone();

            if (IsComplete() && !cloned.IsValid())
            {
                cloned.SourceType = cloned.ResolverType;
            }

            return cloned;
        }

        public static ResolverTypeBindingBuilder New() =>
            new ResolverTypeBindingBuilder();
    }

}
